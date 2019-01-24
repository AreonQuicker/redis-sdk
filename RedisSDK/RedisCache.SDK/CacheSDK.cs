using Cache.Extensions;
using Cache.Objects;
using RedisCache.Store;
using System.Collections.Generic;
using System.Linq;


namespace RedisCache.SDK
{
    public class CacheSDK : BaseCacheSDK
    {
        public CacheSDK(Dictionary<ConfigurationType, object> configurations, bool initCacheStore, bool useInternalCacheStore)
            : base(configurations, initCacheStore, useInternalCacheStore)
        {

        }

        #region Public Methods
        public override void DeleteAllInternal<T>()
        {
            if (internalCacheStore != null)
                internalCacheStore.DeleteAll<T>();
        }

        public override void FlushALLInternal()
        {
            if (internalCacheStore != null)
                internalCacheStore.FlushALL();
        }

        public override IList<T> Get<T>(string[] keys)
        {
            if (internalCacheStore != null)
            {
                RemoveAllExpiredInternal<T>();

                var values = internalCacheStore.Get<T>(keys);

                if (values.Count != keys.Length)
                {
                    values = cacheStore.Get<T>(keys);

                    if (values.Any())
                        internalCacheStore.SetAll((IEnumerable<object>)values);
                }

                return values;
            }
            else
            {
                return cacheStore.Get<T>(keys);
            }
        }

        public override T Get<T>(string key)
        {
            if (internalCacheStore != null)
            {
                RemoveAllExpiredInternal<T>();

                var value = internalCacheStore.Get<T>(key);

                if (value == null)
                {
                    value = cacheStore.Get<T>(key);

                    if (value != null)
                        internalCacheStore.Set<T>(key, value);

                }

                return value;
            }
            else
            {
                return cacheStore.Get<T>(key);
            }
        }

        public override IList<T> GetAll<T>()
        {
            if (internalCacheStore != null)
            {
                RemoveAllExpiredInternal<T>();

                var values = internalCacheStore.GetAll<T>(typeof(T).Name);

                if (!values.Any())
                {
                    values = cacheStore.GetAll<T>();

                    if (values.Any())
                        internalCacheStore.SetAll((IEnumerable<object>)values, typeof(T).Name);

                }

                return values;
            }
            else
            {
                return cacheStore.GetAll<T>();
            }
        }

        public override IList<T> GetAll<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues)
        {
            if (sortFields == null)
                sortFields = new string[0];

            if (internalCacheStore == null)
            {
                return cacheStore.GetAll<T>(connectionType, sortFields, take, connectionValues)
                     .SortAndTake(take, sortFields)
                     .ToList();
            }

            RemoveAllExpiredInternal<T>();

            IList<T> values = new List<T>();

            var hashKey = connectionValues.Aggregate(0, (v, n) => v + n.GetHashCode())
                .ToString();

            hashKey += string.Join(",", sortFields);

            values = internalCacheStore.GetAll<T>(connectionType, hashKey, sortFields, take, connectionValues);

            if (values.Any())
            {
                return values
                    .SortAndTake(take, sortFields)
                    .ToList();
            }

            var redisKeys = cacheStore.GetAllKeys<T>(connectionType, sortFields, take, connectionValues);

            var key = $"{typeof(T).Name}-{connectionValues.GetType().Name}";

            values = internalCacheStore.GetAll<T>(connectionType, key, sortFields, take, connectionValues);

            if (values.Count >= redisKeys.Count)
            {
                internalCacheStore.SetAll((IEnumerable<object>)values, hashKey);

                return values
                    .SortAndTake(take, sortFields)
                    .ToList();
            }

            values = cacheStore.Get<T>(redisKeys.ToArray());

            if (values.Any())
            {
                internalCacheStore.SetAll((IEnumerable<object>)values, key);
                internalCacheStore.SetAll((IEnumerable<object>)values, hashKey);
            }

            return values
                   .SortAndTake(take, sortFields)
                   .ToList();

        }

        public override IList<T> GetAllMultiple<T>()
        {
            if (internalCacheStore != null)
            {
                RemoveAllExpiredInternal<T>();

                var values = internalCacheStore.GetAll<T>(typeof(T).Name);

                if (!values.Any())
                {
                    values = cacheStore.GetAllMultiple<T>();

                    if (values.Any())
                        internalCacheStore.SetAll((IEnumerable<object>)values, typeof(T).Name);

                }

                return values;
            }
            else
            {
                return cacheStore.GetAll<T>();
            }
        }

        public override void Set(object value)
        {
            cacheStore.Set(value);

            if (internalCacheStore != null)
                internalCacheStore.Set(value);
        }

        public override void Set<T>(string key, T value)
        {
            cacheStore.Set<T>(key, value);

            if (internalCacheStore != null)
                internalCacheStore.Set<T>(key, value);
        }

        public override void Set<T>(T value)
        {
            cacheStore.Set(value);

            if (internalCacheStore != null)
                internalCacheStore.Set<T>(value);
        }

        public override void SetAll(IEnumerable<object> values, bool addMultiple)
        {
            cacheStore.SetAll(values, addMultiple);

            if (internalCacheStore != null)
                internalCacheStore.SetAll(values);
        }

        public override void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple)
        {
            cacheStore.SetAll<T>(values, addMultiple);

            if (internalCacheStore != null)
                internalCacheStore.SetAll<T>(values);
        }

        public override void SetInternal(object value)
        {
            if (internalCacheStore != null)
                internalCacheStore.Set(value);
        }

        public override void SetInternal<T>(string key, T value)
        {
            if (internalCacheStore != null)
                internalCacheStore.Set<T>(key, value);
        }

        public override void SetInternal<T>(T value)
        {
            if (internalCacheStore != null)
                internalCacheStore.Set<T>(value);
        }

        public override void SetInternalAll(IEnumerable<object> values)
        {
            if (internalCacheStore != null)
                internalCacheStore.SetAll(values);
        }

        public override void SetInternalAll<T>(IEnumerable<KeyValuePair<string, T>> values)
        {
            if (internalCacheStore != null)
                internalCacheStore.SetAll<T>(values);
        }
        #endregion
    }

}
