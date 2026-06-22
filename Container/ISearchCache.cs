using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Container
{
    public interface ISearchCache
    {
        Task<List<NominatimResult>> GetCachedResultsAsync(string query, int maxResults);
        Task AddToCacheAsync(NominatimResult result);
        Task ClearCacheAsync();
    }
}
