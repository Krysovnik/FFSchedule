using FFSchedule.Container;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FFSchedule.Class
{
    public class JsonFileRouteCache : IRouteCache
    {
        private readonly string _filePath;
        private const int MaxCacheEntries = 100;
        public JsonFileRouteCache()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                , "FFSchedule");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "routes_cache.json");
        }
        public async Task<Dictionary<string, List<RouteResult>>> LoadCacheAsync()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, List<RouteResult>>();

            try
            {
                using var stream = File.OpenRead(_filePath);
                var data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<RouteResult>>>(stream);
                return data ?? new Dictionary<string, List<RouteResult>>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при чтении кэша маршрутов: {ex.Message}");
                return new Dictionary<string, List<RouteResult>>();
            }
        }
        public async Task SaveCacheAsync(Dictionary<string, List<RouteResult>> cacheData)
        {
            try
            {
                while(cacheData.Count > MaxCacheEntries)
                {
                    var firstKey = cacheData.Keys.FirstOrDefault();
                    if (firstKey != null) cacheData.Remove(firstKey);
                }
                using var stream = File.Create(_filePath);
                var options = new JsonSerializerOptions { WriteIndented = true };
                await JsonSerializer.SerializeAsync(stream, cacheData, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при записи кэша маршрутов: {ex.Message}");
            }
        }
        public Task ClearCacheAsync()
        {
            if(File.Exists(_filePath))
                File.Delete(_filePath);
            return Task.CompletedTask;
        }
    }
}
