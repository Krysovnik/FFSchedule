using FFSchedule.Container;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;


namespace FFSchedule.Services
{
    public class RouteService
    {
        private readonly HttpClient httpClient;
        private readonly IEnumerable<FireStation> fireStations;
        private readonly IRouteCache _fileCache;

        private readonly HashSet<FireStation> _usedStations = new HashSet<FireStation>();
        private (double Lat, double Lon)? _lastTargetLocation;

        private Dictionary<string, List<RouteResult>> _memoryCache = new();
        private bool _isCacheLoaded = false;
        public int UsedStationsCount => _usedStations.Count;

        public RouteService(HttpClient httpClient, IEnumerable<FireStation> fireStations, IRouteCache fileCache)
        {
            this.httpClient = httpClient;
            this.fireStations = fireStations;
            _fileCache = fileCache;
        }

        private async Task EnsureCacheLoadedAsync()
        {
            if (!_isCacheLoaded)
            {
                _memoryCache = await _fileCache.LoadCacheAsync();
                _isCacheLoaded = true;
            }
        }

        private string GetCacheKey(double lat, double lon, int requiredEquipment)
        {
            return $"{Math.Round(lat, 5).ToString(CultureInfo.InvariantCulture)};" +
                   $"{Math.Round(lon, 5).ToString(CultureInfo.InvariantCulture)};" +
                   $"{requiredEquipment}";
        }

        public async Task<List<RouteResult>> BuildRoutesByRequirementAsync(double toLat, double toLon, int requiredEquipment)
        {
            _lastTargetLocation = (toLat, toLon);
            string cacheKey = GetCacheKey(toLat, toLon, requiredEquipment);

            await EnsureCacheLoadedAsync();

            if (_memoryCache.TryGetValue(cacheKey, out var cachedResults))
            {
                System.Diagnostics.Debug.WriteLine("Маршрут взят из локального кэша");
                _usedStations.Clear();

                foreach (var res in cachedResults)
                {
                    if (res.Station != null)
                    {
                        var station = fireStations.FirstOrDefault(s =>
                            s.Name == res.Station.Name && s.Address == res.Station.Address);

                        _usedStations.Add(station ?? res.Station);
                        if (station != null) res.Station = station;
                    }
                }
                return cachedResults.ToList();
            }

            var results = new List<RouteResult>();
            try
            {
                _usedStations.Clear();
                var sortedStations = await GetSortedFireStations(toLat, toLon);

                int currentEquipmentSum = 0;
                var selectedStations = new List<FireStation>();

                foreach (var station in sortedStations)
                {
                    if (currentEquipmentSum >= requiredEquipment) break;

                    selectedStations.Add(station);
                    currentEquipmentSum += station.EquipmentCapacity;
                }

                for (int i = 0; i < selectedStations.Count; i++)
                {
                    var station = selectedStations[i];
                    _usedStations.Add(station);

                    var result = await BuildRouteInternalAsync(station.Latitude, station.Longitude, toLat, toLon);
                    result.Station = station;
                    results.Add(result);
                }

                _memoryCache[cacheKey] = results;
                await _fileCache.SaveCacheAsync(_memoryCache);
            }
            catch (Exception ex)
            {
                results.Add(new RouteResult { Success = false, ErrorMessage = ex.Message });
            }
            return results;
        }

        public async Task<RouteResult?> BuildNextAdditionalRouteAsync(double toLat, double toLon)
        {
            var sortedStations = await GetSortedFireStations(toLat, toLon);
            var nextStation = sortedStations.FirstOrDefault(s => !_usedStations.Contains(s));

            if (nextStation == null)
            {
                throw new Exception("Все доступные пожарные части уже задействованы на карте.");
            }

            _usedStations.Add(nextStation);

            var result = await BuildRouteInternalAsync(nextStation.Latitude, nextStation.Longitude, toLat, toLon);
            result.Station = nextStation;

            return result;
        }

        public void ResetActiveRouteState()
        {
            _usedStations.Clear();
            _lastTargetLocation = null;
        }

        public async Task ClearCache()
        {
            _memoryCache.Clear();
            await _fileCache.ClearCacheAsync();
        }

        private async Task<List<FireStation>> GetSortedFireStations(double toLat, double toLon)
        {
            var coordinates = string.Join(";",
                fireStations.Select(s => $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}")) +
                $";{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string tableUrl = $"http://router.project-osrm.org/table/v1/driving/{Uri.EscapeDataString(coordinates)}";

            var response = await httpClient.GetAsync(tableUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            var durationsElement = json.RootElement.GetProperty("durations");
            var durations = durationsElement.EnumerateArray().ToList();

            int targetIndex = durations.Count - 1;

            return fireStations
                .Select((station, index) =>
                {
                    var durationRow = durations[index].EnumerateArray().ToList();
                    var durationToTarget = durationRow[targetIndex].GetDouble();
                    return new { Station = station, Duration = durationToTarget };
                })
                .OrderBy(x => x.Duration)
                .Select(x => x.Station)
                .ToList();
        }

        private async Task<RouteResult> BuildRouteInternalAsync(double fromLat, double fromLon, double toLat, double toLon)
        {
            string coordinates = $"{fromLon.ToString(CultureInfo.InvariantCulture)},{fromLat.ToString(CultureInfo.InvariantCulture)};" +
                                 $"{toLon.ToString(CultureInfo.InvariantCulture)},{toLat.ToString(CultureInfo.InvariantCulture)}";

            string osrmUrl = $"http://router.project-osrm.org/route/v1/driving/{Uri.EscapeDataString(coordinates)}?overview=full&geometries=geojson";

            var response = await httpClient.GetAsync(osrmUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            var routeGeometry = json.RootElement.GetProperty("routes")[0].GetProperty("geometry").GetProperty("coordinates");
            var distance = json.RootElement.GetProperty("routes")[0].GetProperty("distance").GetDouble();
            var duration = json.RootElement.GetProperty("routes")[0].GetProperty("duration").GetDouble();

            var rawCoordsDto = routeGeometry.EnumerateArray()
                .Select(c => new CoordinateDto { X = c[0].GetDouble(), Y = c[1].GetDouble() })
                .ToList();

            return new RouteResult { Success = true, Distance = distance, Duration = duration, RawCoordinates = rawCoordsDto };
        }
    }
}
