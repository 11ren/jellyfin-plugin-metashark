﻿using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.MetaShark.Api.Http;
using Jellyfin.Plugin.MetaShark.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MetaShark.Api
{
    public class OmdbApi : IDisposable
    {
        public const string DEFAULT_API_KEY = "2c9d9507";
        private readonly ILogger<DoubanApi> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient httpClient;

        public OmdbApi(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DoubanApi>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<OmdbItem?> GetByImdbID(string id, CancellationToken cancellationToken)
        {
            if (!this.IsEnable())
            {
                return null;
            }

            var cacheKey = $"GetByImdbID_{id}";
            var expiredOption = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };
            OmdbItem? item;
            if (this._memoryCache.TryGetValue(cacheKey, out item))
            {
                return item;
            }

            try
            {
                var url = $"http://www.omdbapi.com/?i={id}&apikey={DEFAULT_API_KEY}";
                item = await this.httpClient.GetFromJsonAsync<OmdbItem>(url, cancellationToken).ConfigureAwait(false);
                _memoryCache.Set(cacheKey, item, expiredOption);
                return item;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "GetByImdbID error. id: {0}", id);
                _memoryCache.Set<OmdbItem?>(cacheKey, null, expiredOption);
                return null;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryCache.Dispose();
            }
        }

        private bool IsEnable()
        {
            return Plugin.Instance?.Configuration.EnableTmdb ?? true;
        }
    }
}
