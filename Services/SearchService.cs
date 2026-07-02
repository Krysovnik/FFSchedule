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
        private readonly ISearchCache _cache;
        private const int MaxTotalResults = 5;

        public SearchService(HttpClient httpClient, ISearchCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task AddToHistory(NominatimResult result)
        {
            await _cache.AddToCacheAsync(result);
        }

        public async Task<List<NominatimResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<NominatimResult>();

            string cleanedQuery = query.Trim().ToLower();
            var finalResults = await _cache.GetCachedResultsAsync(cleanedQuery, MaxTotalResults);

            if (finalResults.Count < MaxTotalResults)
            {
                int remainingSlots = MaxTotalResults - finalResults.Count;

                string viewbox = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", 82.55, 55.15, 83.20, 54.80);

                var url = $"https://nominatim.openstreetmap.org/search" +
                          $"?q={Uri.EscapeDataString(query.Trim())}" +
                          $"&format=jsonv2" +
                          $"&addressdetails=1" +
                          $"&extratags=1" +
                          $"&countrycodes=RU" +
                          $"&viewbox={viewbox}" +
                          $"&bounded=1" +
                          $"&limit={MaxTotalResults}";
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

        public async Task<NominatimResult?> ReverseSearchAsync(double lat, double lon)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);

            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=jsonv2&addressdetails=1";

            try
            {
                return await _httpClient.GetFromJsonAsync<NominatimResult>(url);
            }
            catch
            {
                return null;
            }
        }

        public async Task ClearCache() => await _cache.ClearCacheAsync();
    }
}
