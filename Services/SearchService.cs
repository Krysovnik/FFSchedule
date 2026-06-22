using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Wpf;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Globalization;
using FFSchedule.Container;

namespace FFSchedule.Services
{
    public class SearchService
    {
        private readonly HttpClient _httpClient;
        private readonly MapControl _mapControl;
        private readonly ISearchCache _cache;
        private const string SEARCH_PIN_LAYER = "SearchPin";

        //private readonly List<NominatimResult> _searchHistory = new List<NominatimResult>();

        private const int maxTotalResults = 5;

        public SearchService(HttpClient httpClient, MapControl mapControl, ISearchCache cache)
        {
            _httpClient = httpClient;
            _mapControl = mapControl;
            _cache = cache;
        }


        #region Публичные методы

        public async Task AddToHistory(NominatimResult result)
        {
            await _cache.AddToCacheAsync(result);
        }

        /// Выполняет поиск по запросу и возвращает отсортированный список результатов.
        public async Task<List<NominatimResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<NominatimResult>();         

            string cleanedQuery = query.Trim().ToLower();

            var finalResults = await _cache.GetCachedResultsAsync(cleanedQuery, maxTotalResults);

            if (finalResults.Count < maxTotalResults)
            {
                int remainingSlots = maxTotalResults - finalResults.Count;

                string viewbox = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}",
                82.55, 55.15, 83.20, 54.80);

                var url = $"https://nominatim.openstreetmap.org/search" +
                          $"?q={Uri.EscapeDataString(query.Trim())}" +
                          $"&format=jsonv2" +
                          $"&addressdetails=1" +
                          $"&extratags=1" +
                          $"&countrycodes=RU" +
                          $"&viewbox={viewbox}" +
                          $"&bounded=1" +
                          $"&limit={maxTotalResults}";
                try
                {
                    var networkResults = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);
                    if (networkResults != null)
                    {
                        var uniqueNetworkResults = networkResults
                            .Where(nr => !finalResults.Any(fr => fr.DisplayName == nr.DisplayName))
                            .OrderByDescending(r => r.Importance)
                            .Take(remainingSlots);

                        finalResults.AddRange(uniqueNetworkResults);
                    }
                }
                catch (HttpRequestException)
                {
                    System.Diagnostics.Debug.WriteLine("Nominatim недоступен. Выведены только результаты из кэша.");
                }
            }
            return finalResults;
        }
 
        public void PutSearchPin(NominatimResult result)
        {
            var merc = Mapsui.Projections.SphericalMercator.FromLonLat(result.Lon, result.Lat);

            var old = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == SEARCH_PIN_LAYER);
            if (old != null) _mapControl?.Map?.Layers.Remove(old);

            var pin = new GeometryFeature
            {
                Geometry = new Point(merc.x, merc.y),
                ["label"] = result.DisplayName,
                ["shortLabel"] = result.ShortDisplayName
            };

            pin.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Rectangle,
                Fill = new Brush(Color.FromArgb(255, 255, 143, 0)),
                Outline = new Pen(new Color(255, 255, 255, 255), 3.0f) { PenStyle = PenStyle.Solid },
                SymbolScale = 0.35f,
                MinVisible = 1,
                MaxVisible = 500
            });

            pin.Styles.Add(new LabelStyle
            {
                Text = result.ShortDisplayName,
                Font = new Font { Size = 12, Bold = true, FontFamily = "Arial" },
                ForeColor = new Color(0, 0, 0),
                BackColor = new Brush(new Color(255, 255, 255, 220)),
                Halo = new Pen(new Color(255, 255, 255, 200), 1.5f),
                Offset = new Offset(0.0, -18),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                LineHeight = 1.2,
                MaxVisible = 80
            });

            _mapControl?.Map?.Layers.Add(new MemoryLayer
            {
                Name = SEARCH_PIN_LAYER,
                Features = new[] { pin },
                Style = null
            });
        }

        /// Перемещает карту к координатам результата и ставит маркер.
        public void FlyToResult(NominatimResult result)
        {
            var merc = Mapsui.Projections.SphericalMercator.FromLonLat(result.Lon, result.Lat);
            _mapControl.Map?.Navigator?.CenterOn(merc.x, merc.y);
            _mapControl.Map?.Navigator?.ZoomToLevel(15);
            PutSearchPin(result);
        }
        public async Task<NominatimResult?> ReverseSearchAsync(double lat, double lon)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);

            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=jsonv2&addressdetails=1";

            try
            {
                var result = await _httpClient.GetFromJsonAsync<NominatimResult>(url);
                return result;
            }
            catch { return null; }
        }

        public void RemoveSearchPin()
        {
            var old = _mapControl.Map?.Layers.FirstOrDefault(l => l.Name == SEARCH_PIN_LAYER);
            if (old != null)
                _mapControl?.Map?.Layers.Remove(old);
        }

        public async Task ClearCache() => await _cache.ClearCacheAsync();
        #endregion
    }
}
