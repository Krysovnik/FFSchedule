using System;
using System.Collections.Generic;
using System.Text;

namespace FFSchedule.Container
{
    public interface IRouteCache
    {
        Task<Dictionary<string, List<RouteResult>>> LoadCacheAsync();
        Task SaveCacheAsync(Dictionary<string, List<RouteResult>> cacheData);
        Task ClearCacheAsync();
    }
}
