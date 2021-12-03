using System;
using azure_auth_and_arm_demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace azure_auth_and_arm_demo.Caches
{
    public class SessionCache
    {
        private IMemoryCache _cache;

        public SessionCache()
        {
            _cache = new MemoryCache(
                new MemoryCacheOptions
                {
                    SizeLimit = 1024
                }
            );
        }

        public void CreateEntry(string key, Session session)
        {
            using (ICacheEntry entry = _cache.CreateEntry(key))
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                entry.Value = session;
                entry.Size = 1;
            }
        }

        public bool TryGetValue(object key, out Session value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void Remove(object key)
        {
            _cache.Remove(key);
		}

        public Session GetSessionIfAuthenticated(HttpRequest request, ILogger logger)
        {
            string sessionId = request.Cookies["azure_auth_and_arm_demo_session_id"];
            if (sessionId == null || sessionId.Length == 0)
            {
                logger.LogInformation("no session cookie");
                return null;
            }

            Session session;
            bool found = _cache.TryGetValue(sessionId, out session);
            if (!found)
            {
                logger.LogInformation("session not found");
                return null;
            }

            if (session.Token == null)
            {
                logger.LogInformation("token not acquired");
                return null;
            }

            if (session.Token.ExpiresOn < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                logger.LogInformation("token expired");
                _cache.Remove(sessionId);
                return null;
            }

            return session;
        }
	}
}