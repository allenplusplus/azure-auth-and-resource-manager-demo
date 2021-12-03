using System;
using Microsoft.Extensions.Caching.Memory;

namespace azure_auth_and_arm_demo.Caches
{
    public class StateCache
    {
        private IMemoryCache _cache;

        public StateCache()
        {
            _cache = new MemoryCache(
                new MemoryCacheOptions
                {
                    SizeLimit = 1024
                }
            );
        }

        public void CreateEntry(string key, string value)
        {
            using (ICacheEntry entry = _cache.CreateEntry(key))
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                entry.Value = value;
                entry.Size = 1;
            }
        }

        public bool TryGetValue(object key, out string value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Remove(object key)
        {
            _cache.Remove(key);
		}
	}
}