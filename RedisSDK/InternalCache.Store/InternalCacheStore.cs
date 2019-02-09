using Cache.Extensions;
using Cache.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace InternalCache.Store
{
    public class InternalCacheStore : BaseInternalCacheStore
    {
        private ConcurrentDictionary<Type, EntityCache> store = null;

        public InternalCacheStore(TimeSpan defaultCacheTimeSpan, string key)
            : base(defaultCacheTimeSpan, key)
        {
            store = new ConcurrentDictionary<Type, EntityCache>();
        }

        #region Private Methods
        private EntityCache GetAndAddEntityCache<T>()
        {
            return GetAndAddEntityCache(typeof(T));
        }

        private EntityCache GetAndAddEntityCache(Type typeOfCache)
        {
            EntityCache entityCacheStore = null;

            if (store.ContainsKey(typeOfCache))
                entityCacheStore = store[typeOfCache];
            else
            {
                entityCacheStore = new EntityCache(defaultCacheTimeSpan);
                store.TryAdd(typeOfCache, entityCacheStore);
            }

            return entityCacheStore;
        }

        private EntityCache GetEntityCache<T>()
        {
            return GetEntityCache(typeof(T));
        }

        private EntityCache GetEntityCache(Type typeOfCache)
        {
            if (store.ContainsKey(typeOfCache))
                return store[typeOfCache];

            return null;
        }
        #endregion

        #region Public Methods
        public override void DeleteAll<T>()
        {
            Type typeOfCache = typeof(T);

            store.TryRemove(typeOfCache, out EntityCache val);
        }

        public override void FlushALL()
        {
            store = new ConcurrentDictionary<Type, EntityCache>();
        }

        public override IList<T> Get<T>(string[] keys)
        {
            return keys
                .Select(s => Get<T>(s))
                .Where(w => w != null)
                .ToList();
        }

        public override T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default(T);

            var entityCacheStore = GetEntityCache<T>();

            if (entityCacheStore != null)
            {
                var value = entityCacheStore.Get(key);

                if (value != null
                    && (value.DateAdded.Add(defaultCacheTimeSpan) > DateTime.Now))
                    return (T)value.Value;
            }

            return default(T);
        }

        public override IList<T> GetAll<T>(string eKey)
        {
            EntityCache entityCacheStore = GetEntityCache<T>();

            if (entityCacheStore != null)
            {
                return entityCacheStore.Values(eKey)
                    .Where(w => (w.DateAdded.Add(defaultCacheTimeSpan) > DateTime.Now))
                    .Select(s => s.Value)
                    .Distinct()
                    .Cast<T>()
                    .ToList();
            }

            return new List<T>();
        }

        public override IList<T> GetAll<T>()
        {
            return GetAll<T>(null);
        }

        public override void Set(object value)
        {
            Type typeOfCache = value.GetType();

            EntityCache entityCacheStore = GetAndAddEntityCache(typeOfCache);

            entityCacheStore.Set(value, propertyKey);
        }

        public override void Set<T>(string key, T value)
        {
            EntityCache entityCacheStore = GetAndAddEntityCache<T>();

            entityCacheStore.Set(key, value);
        }

        public override void Set<T>(T value)
        {
            EntityCache entityCacheStore = GetAndAddEntityCache<T>();

            entityCacheStore.Set(value, propertyKey);
        }

        public override void SetAll(IEnumerable<object> values, string eKey)
        {
            if (values.Any())
            {
                Type typeOfCache = values.FirstOrDefault().GetType();

                EntityCache entityCacheStore = GetAndAddEntityCache(typeOfCache);

                foreach (var value in values)
                    entityCacheStore.Set(value, eKey, propertyKey);
            }
        }

        public override void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, string eKey)
        {
            EntityCache entityCacheStore = GetAndAddEntityCache<T>();

            foreach (var value in values)
                entityCacheStore.Set(value.Key, value.Value, eKey);
        }

        public override void SetAll(IEnumerable<object> values)
        {
            SetAll(values, null);
        }

        public override void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values)
        {
            SetAll<T>(values, null);
        }

        public override IList<T> GetAll<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues)
        {
            return GetAll<T>(connectionType, null, sortFields, take, connectionValues);
        }

        public override IList<T> GetAll<T>(ConnectionType connectionType, string eKey, string[] sortFields, int? take, params ConnectionValue[] connectionValues)
        {
            EntityCache entityCacheStore = GetEntityCache<T>();

            if (entityCacheStore != null)
            {
                var connectionValuesD = connectionValues
                    .GroupBy(g => g.Field)
                    .ToDictionary(d => d.Key, d => d.FirstOrDefault());

                var values = entityCacheStore.Values(eKey)
                    .AsParallel()
                    .Select(s =>
                    {
                        return new
                        {
                            s.Value,
                            values = connectionValuesD
                            .ToDictionary(
                                d => d.Key,
                                d => s.Value.GetValueFromObject(d.Key)
                                )
                        };
                    })
                    .Where(w =>
                    {
                        if (w.values == null)
                            return false;

                        Func<ConnectionValue, bool> predicate = (a) =>
                        {
                            if (w.values[a.Field] == null)
                                return false;

                            switch (a.ConnectionValueType)
                            {
                                case ConnectionValueType.Contains:
                                    return w.values[a.Field].CaseInsensitiveContains(a.Value);
                                case ConnectionValueType.StartsWith:
                                    return w.values[a.Field].StartsWith(a.Value);
                                case ConnectionValueType.EndWith:
                                    return w.values[a.Field].EndsWith(a.Value);
                                default:
                                    return w.values[a.Field].Equals(a.Value, StringComparison.CurrentCultureIgnoreCase);
                            };
                        };

                        if (connectionType == ConnectionType.And)
                            return connectionValuesD.Values.All(predicate);
                        else
                            return connectionValuesD.Values.Any(predicate);
                    })
                    .Select(s => s.Value)
                    .Distinct()
                    .Cast<T>();

                if (take.HasValue)
                    return values.SortAndTake(take.Value, sortFields)
                        .ToList();
                else
                    return values.Sort(sortFields)
                        .ToList();
            }

            return new List<T>();
        }

        public override void RemoveAllExpired<T>()
        {
            var entityCacheStore = GetEntityCache<T>();

            if (entityCacheStore != null)
            {
                entityCacheStore.RemoveAllExpired();
            }
        }

        #endregion

        #region Private Classes
        private class EntityCache
        {
            private const string defaultKey = "DefaultKey";

            private readonly Dictionary<string, Dictionary<string, EntityCacheItem>> _store = null;
            private readonly TimeSpan _defaultCacheTimeSpan;

            private Dictionary<string, EntityCacheItem> defaultStore => _store[defaultKey];

            public DateTime DateAdded { get; set; }

            public EntityCache(TimeSpan defaultCacheTimeSpan)
            {
                _store = new Dictionary<string, Dictionary<string, EntityCacheItem>>();

                _store.Add(defaultKey, new Dictionary<string, EntityCacheItem>());

                DateAdded = DateTime.Now;

                _defaultCacheTimeSpan = defaultCacheTimeSpan;
            }

            #region Private Methods
            private static string GetKey(object cacheItem, string propertyKey)
            {
                string key = null;
                Type type = cacheItem.GetType();

                var eProperty = type.GetProperty(propertyKey, typeof(string));
                if (eProperty != null)
                    key = eProperty.GetValue(cacheItem) as string;

                return key;
            }
            #endregion

            #region Public Methods		
            public IList<EntityCacheItem> DefaultValues()
            {
                var values = defaultStore
                        .Where(w => (w.Value.DateAdded.Add(_defaultCacheTimeSpan) > DateTime.Now))
                        .Select(s => s.Value)
                        .ToList();

                var keysToBeRemoved = defaultStore
                    .Where(w => (w.Value.DateAdded.Add(_defaultCacheTimeSpan) <= DateTime.Now))
                    .Select(s => s.Key)
                    .ToList();

                if (keysToBeRemoved.Any())
                    keysToBeRemoved.ForEach(f => defaultStore.Remove(f));

                return values.ToList();
            }

            public IList<string> DefaultKeys()
            {
                return defaultStore.Keys.ToList();
            }

            public IList<EntityCacheItem> Values(string eKey)
            {
                if (string.IsNullOrEmpty(eKey))
                    return DefaultValues();

                if (_store.ContainsKey(eKey))
                {
                    var innterStore = _store[eKey];

                    var values = innterStore
                        .Where(w => (w.Value.DateAdded.Add(_defaultCacheTimeSpan) > DateTime.Now))
                        .Select(s => s.Value)
                        .ToList();

                    var keysToBeRemoved = innterStore
                        .Where(w => (w.Value.DateAdded.Add(_defaultCacheTimeSpan) <= DateTime.Now))
                        .Select(s => s.Key)
                        .ToList();

                    if (keysToBeRemoved.Any())
                        keysToBeRemoved.ForEach(f => innterStore.Remove(f));

                    return values.ToList();
                }

                return new List<EntityCacheItem>();
            }

            public IList<string> Keys(string eKey)
            {
                if (string.IsNullOrEmpty(eKey))
                    return DefaultKeys();

                if (_store.ContainsKey(eKey))
                {
                    var innterStore = _store[eKey];

                    return innterStore.Keys.ToList();
                }

                return new List<string>();
            }

            public EntityCacheItem Get(string key)
            {
                if (defaultStore.ContainsKey(key))
                {
                    var v = defaultStore[key];

                    if ((v.DateAdded.Add(_defaultCacheTimeSpan) > DateTime.Now))
                        return v;

                    defaultStore.Remove(key);
                }

                return null;
            }

            public EntityCacheItem Get(string key, string eKey)
            {
                if (_store.ContainsKey(eKey)
                    && _store[eKey].ContainsKey(key))
                {
                    var v = _store[eKey][key];

                    if ((v.DateAdded.Add(_defaultCacheTimeSpan) > DateTime.Now))
                        return v;

                    _store[eKey].Remove(key);
                }

                return null;
            }

            public void Set(string key, object cacheItem)
            {
                if (defaultStore.ContainsKey(key))
                    defaultStore.Remove(key);

                defaultStore.Add(key, new EntityCacheItem(cacheItem));
            }

            public void Set(string key, object cacheItem, string eKey)
            {
                if (string.IsNullOrEmpty(eKey))
                {
                    Set(key, cacheItem);

                    return;
                }

                if (!_store.ContainsKey(eKey))
                    _store.Add(eKey, new Dictionary<string, EntityCacheItem>());

                if (_store[eKey].ContainsKey(key))
                    _store[eKey].Remove(key);

                _store[eKey].Add(key, new EntityCacheItem(cacheItem));
            }

            public void Set(object cacheItem, string propertyKey)
            {
                string key = GetKey(cacheItem, propertyKey);

                if (key != null)
                {
                    if (defaultStore.ContainsKey(key))
                        defaultStore.Remove(key);

                    defaultStore.Add(key, new EntityCacheItem(cacheItem));
                }
            }

            public void Set(object cacheItem, string eKey, string propertyKey)
            {
                if (string.IsNullOrEmpty(eKey))
                {
                    Set(cacheItem, propertyKey);

                    return;
                }

                string key = GetKey(cacheItem, propertyKey);

                if (key != null)
                {
                    if (!_store.ContainsKey(eKey))
                        _store.Add(eKey, new Dictionary<string, EntityCacheItem>());

                    if (_store[eKey].ContainsKey(key))
                        _store[eKey].Remove(key);

                    _store[eKey].Add(key, new EntityCacheItem(cacheItem));
                }
            }

            public void RemoveAllExpired()
            {
                foreach (var storeItem in _store)
                {
                    var keysToBeRemoved = storeItem.Value
                    .Where(w => (w.Value.DateAdded.Add(_defaultCacheTimeSpan) > DateTime.Now))
                    .Select(s => s.Key)
                    .ToList();

                    if (keysToBeRemoved.Any())
                        keysToBeRemoved.ForEach(f => storeItem.Value.Remove(f));
                }
            }
            #endregion
        }

        private class EntityCacheItem
        {
            #region Public Properties
            public DateTime DateAdded { get; set; }
            public object Value { get; } = null;
            #endregion

            public EntityCacheItem(object cacheItem)
            {
                Value = cacheItem;
                DateAdded = DateTime.Now;
            }
        }
        #endregion
    }

}
