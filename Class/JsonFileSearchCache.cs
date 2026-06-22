using FFSchedule.Container;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FFSchedule.Class
{
    public class JsonFileSearchCache : ISearchCache
    {
        private readonly string? _filePath;

        public JsonFileSearchCache()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapSearchApp");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "search_history.json");
        }
        private async Task<List<NominatimResult>> LoadAllAsync()
        {
            if (!File.Exists(_filePath)) return new List<NominatimResult>();
            try
            {
                using var stream = File.OpenRead(_filePath);
                return await JsonSerializer.DeserializeAsync<List<NominatimResult>>(stream) ?? new List<NominatimResult>();
            }
            catch { return new List<NominatimResult>(); }
        }
        public async Task<List<NominatimResult>> GetCachedResultsAsync(string query, int maxResults)
        {
            var all = await LoadAllAsync();
            return all.Where(h => h.DisplayName.ToLower().Contains(query) ||
                                 (h.ShortDisplayName != null && h.ShortDisplayName.ToLower().Contains(query)))
                      .Take(maxResults).ToList();
        }

        public async Task AddToCacheAsync(NominatimResult result)
        {
            var all = await LoadAllAsync();
            if (!all.Any(h => h.DisplayName == result.DisplayName))
            {
                all.Add(result);
                using var stream = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(stream, all);
            }
        }

        public Task ClearCacheAsync()
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
            return Task.CompletedTask;
        }
    }
}
