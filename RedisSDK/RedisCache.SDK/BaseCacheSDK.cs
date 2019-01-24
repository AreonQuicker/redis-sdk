using Cache.Objects;
using InternalCache.Store;
using RedisCache.Store;
using System;
using System.Collections.Generic;

namespace RedisCache.SDK
{
    public abstract class BaseCacheSDK : ICacheSDK
    {
        protected IInternalCacheStore internalCacheStore;
        protected ICacheStore cacheStore;
        protected DateTime date;
        protected TimeSpan defaultInternalCacheTimeSpan;

        protected BaseCacheSDK(Dictionary<ConfigurationType, object> configurations, bool initCacheStore, bool useInternalCacheStore)
        {
            if (useInternalCacheStore)
            {
                defaultInternalCacheTimeSpan = (TimeSpan)configurations[ConfigurationType.DefaultInternalCacheTimeSpan];

                internalCacheStore = new InternalCacheStore(defaultInternalCacheTimeSpan,
                    configurations.ContainsKey(ConfigurationType.Key)
                    ? configurations[ConfigurationType.Key].ToString() : "CacheKey");

                date = DateTime.Now;
            }

            cacheStore = new CacheStore(configurations, initCacheStore);
        }

        #region Protected Methods
        protected void RemoveAllExpiredInternal<T>()
        {
            if (internalCacheStore != null
                && date.Add(defaultInternalCacheTimeSpan) < DateTime.Now)
            {
                internalCacheStore.RemoveAllExpired<T>();
                date = DateTime.Now;
            }
        }
        #endregion

        #region Public Methods
        public void InitCache(string host, int? port)
        {
            if (!string.IsNullOrEmpty(host))
                cacheStore.Init(host, port);
            else
                cacheStore.Init();
        }
        #endregion

        #region Public Abstract Methods
        public abstract void DeleteAllInternal<T>();
        public abstract void FlushALLInternal();
        public abstract IList<T> Get<T>(string[] keys);
        public abstract T Get<T>(string key);
        public abstract IList<T> GetAll<T>();
        public abstract IList<T> GetAll<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues);
        public abstract void SetInternal(object value);
        public abstract void SetInternal<T>(string key, T value);
        public abstract void SetInternal<T>(T value);
        public abstract void SetInternalAll(IEnumerable<object> values);
        public abstract void SetInternalAll<T>(IEnumerable<KeyValuePair<string, T>> values);
        public abstract IList<T> GetAllMultiple<T>();
        public abstract void Set(object value);
        public abstract void Set<T>(string key, T value);
        public abstract void Set<T>(T value);
        public abstract void SetAll(IEnumerable<object> values, bool addMultiple);
        public abstract void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple);
        #endregion
    }

}
