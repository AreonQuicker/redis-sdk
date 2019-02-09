using Cache.Extensions;
using Cache.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    public class CacheStore : BaseCacheStore
    {
        #region Private ReadOnly Variables
        private readonly CacheDictionary<Type> mainCacheDictionary;
        #endregion

        public CacheStore(Dictionary<ConfigurationType, object> configurations, bool init = false)
            : base(configurations, init)
        {
            mainCacheDictionary = new CacheDictionary<Type>(connectionMultiplexer, AppKey);
        }

        #region Private Methods
        private CacheDictionary<List<string>> ConnectionCacheDictionaryInstance(Type type) =>
        new CacheDictionary<List<string>>(connectionMultiplexer, ConnectionTypeKey(type.Name));

        private CacheDictionary<List<string>> ConnectionCacheDictionaryInstance<T>() =>
          new CacheDictionary<List<string>>(connectionMultiplexer, ConnectionTypeKey<T>());

        private CacheDictionary<T> CacheDictionaryInstance<T>() =>
            new CacheDictionary<T>(connectionMultiplexer, TypeKey<T>());

        private CacheDictionary<object> CacheDictionaryInstance(Type type) =>
          new CacheDictionary<object>(connectionMultiplexer, TypeKey(type.Name));
        #endregion

        #region Public Methods
        public override void DeleteAll<T>()
        {
            lock (lockObject)
            {
                var cacheDictionary = CacheDictionaryInstance<T>();

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

                if (mainCacheDictionary.ContainsKey(TypeKey<T>()))
                    mainCacheDictionary.Remove(TypeKey<T>());

                cacheDictionary.Clear();

                connectionCacheDictionary.Clear();
            }
        }

        public override void DeleteAll(Type type)
        {
            lock (lockObject)
            {
                var cacheDictionary = CacheDictionaryInstance(type);

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance(type);

                if (mainCacheDictionary.ContainsKey(TypeKey(type.Name)))
                    mainCacheDictionary.Remove(TypeKey(type.Name));

                cacheDictionary.Clear();

                connectionCacheDictionary.Clear();
            }
        }

        public override void FlushALL()
        {
            var server = connectionMultiplexer.GetServer(redisServerConnection);

            server.FlushDatabase();
        }

        public override IList<T> Get<T>(string[] keys)
        {
            keys = keys
                .Select(s => Key<T>(s))
                .ToArray();

            var values = CacheDictionaryInstance<T>().GetValues(keys);

            if (values == null)
                return new List<T>();

            return values;
        }

        public override T Get<T>(string key)
        {
            return CacheDictionaryInstance<T>().GetValue(Key<T>(key));
        }

        public override IList<T> GetAll<T>()
        {
            var keys = CacheDictionaryInstance<T>().Keys;

            if (keys == null)
                return new List<T>();

            var values = CacheDictionaryInstance<T>().GetValues(keys.ToArray());

            if (values == null)
                return new List<T>();

            return values;
        }

        public override IList<T> GetAll<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues)
        {
            return Get<T>(GetAllKeys<T>(connectionType, sortFields, take, connectionValues).ToArray());
        }

        public override IList<Type> GetAllTypes()
        {
            return mainCacheDictionary
                .Values
                .ToList();
        }

        public override void Remove<T>(string key)
        {
            lock (lockObject)
            {
                var cacheDictionary = CacheDictionaryInstance<T>();

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

                cacheDictionary.Remove(key);

                var keysToBeDeleted = new List<string>();

                foreach (var kv in connectionCacheDictionary)
                {
                    if (kv.Value.ToString() == key)
                        keysToBeDeleted.Add(kv.Key);
                }

                Parallel.ForEach(keysToBeDeleted, redisKey => connectionCacheDictionary.Remove(redisKey));
            }
        }

        public override void Remove<T>(string[] keys)
        {
            lock (lockObject)
            {
                var cacheDictionary = CacheDictionaryInstance<T>();

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

                foreach (var key in keys)
                    cacheDictionary.Remove(key);

                var keysToBeDeleted = new List<string>();

                foreach (var kv in connectionCacheDictionary)
                {
                    if (keys.Contains(kv.Value.ToString()))
                        keysToBeDeleted.Add(kv.Key);
                }

                Parallel.ForEach(keysToBeDeleted, redisKey => connectionCacheDictionary.Remove(redisKey));
            }
        }

        public override void Remove(string[] keys, Type type)
        {
            lock (lockObject)
            {
                var cacheDictionary = CacheDictionaryInstance(type);

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance(type);

                foreach (var key in keys)
                    cacheDictionary.Remove(key);

                var keysToBeDeleted = new List<string>();

                foreach (var kv in connectionCacheDictionary)
                {
                    if (keys.Contains(kv.Value.ToString()))
                        keysToBeDeleted.Add(kv.Key);
                }

                Parallel.ForEach(keysToBeDeleted, redisKey => connectionCacheDictionary.Remove(redisKey));
            }
        }

        public override void Set(object value)
        {
            lock (lockObject)
            {
                var type = value.GetType();

                if (!mainCacheDictionary.ContainsKey(TypeKey(type.Name)))
                    mainCacheDictionary.Add(TypeKey(type.Name), type);

                var cacheKey = value.GetValueFromObject(fieldKey);

                if (!string.IsNullOrEmpty(cacheKey))
                    CacheDictionaryInstance(type).Add(Key(cacheKey, type.Name), value);
            }
        }

        public override void Set<T>(string key, T value)
        {
            lock (lockObject)
            {
                if (!mainCacheDictionary.ContainsKey(TypeKey<T>()))
                    mainCacheDictionary.Add(TypeKey<T>(), typeof(T));

                CacheDictionaryInstance<T>().Add(Key<T>(key), value);
            }
        }

        public override void SetAll(IEnumerable<object> values, bool addMultiple)
        {
            lock (lockObject)
            {
                var type = values.FirstOrDefault().GetType();

                if (!mainCacheDictionary.ContainsKey(TypeKey(type.Name)))
                    mainCacheDictionary.Add(TypeKey(type.Name), type);

                var cacheDictionaryInstance = CacheDictionaryInstance(type);

                var redisValues =
                     values
                     .AsParallel()
                     .Select(s => new KeyValuePair<string, object>(Key(s.GetValueFromObject(fieldKey), type.Name), s))
                     .ToList();

                if (addMultiple)
                    cacheDictionaryInstance.AddMultiple(redisValues);
                else
                {
                    foreach (var redisValue in redisValues)
                        cacheDictionaryInstance.Add(redisValue);
                }

                redisValues.Clear();
            }
        }

        public override void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple)
        {
            lock (lockObject)
            {
                if (!mainCacheDictionary.ContainsKey(TypeKey<T>()))
                    mainCacheDictionary.Add(TypeKey<T>(), typeof(T));

                var cacheDictionaryInstance = CacheDictionaryInstance<T>();

                var redisValues = values
                     .AsParallel()
                     .Select(s => new KeyValuePair<string, T>(Key<T>(s.Key), s.Value))
                     .ToList();

                if (addMultiple)
                    cacheDictionaryInstance.AddMultiple(redisValues);
                else
                {
                    foreach (var value in redisValues)
                        cacheDictionaryInstance.Add(value);
                }

                redisValues.Clear();
            }
        }

        public override void Set(object value, string[] connectionsFields)
        {
            lock (lockObject)
            {
                var type = value.GetType();

                if (!mainCacheDictionary.ContainsKey(TypeKey(type.Name)))
                    mainCacheDictionary.Add(TypeKey(type.Name), type);

                var cacheKey = value.GetValueFromObject(fieldKey);

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    var connectionCacheDictionary = ConnectionCacheDictionaryInstance(type);

                    Parallel.ForEach(connectionsFields.Distinct(), (connectionsField) =>
                    {
                        if (value.GetValueFromObject(connectionsField, out var connectionsFieldValue))
                        {
                            var linkKey = LinkKey(connectionsField, connectionsFieldValue);

                            var redisValue = connectionCacheDictionary.GetValue(linkKey);

                            if (redisValue == null)
                                redisValue = new List<string>();

                            redisValue.Add(cacheKey);

                            connectionCacheDictionary.Add(linkKey, redisValue.Distinct().ToList());
                        }
                    });

                    CacheDictionaryInstance(type).Add(Key(cacheKey, type.Name), value);
                }
            }
        }

        public override void Set<T>(string key, T value, string[] connectionsFields)
        {
            lock (lockObject)
            {
                if (!mainCacheDictionary.ContainsKey(TypeKey<T>()))
                    mainCacheDictionary.Add(TypeKey<T>(), typeof(T));

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

                Parallel.ForEach(connectionsFields.Distinct(), (connectionsField) =>
                {
                    if (value.GetValueFromObject(connectionsField, out var connectionsFieldValue))
                    {
                        var linkKey = LinkKey(connectionsField, connectionsFieldValue);

                        var redisValue = connectionCacheDictionary.GetValue(linkKey);

                        if (redisValue == null)
                            redisValue = new List<string>();

                        redisValue.Add(key);

                        connectionCacheDictionary.Add(linkKey, redisValue.Distinct().ToList());
                    }
                });

                CacheDictionaryInstance<T>().Add(Key<T>(key), value);
            }
        }

        public override void SetAll(IEnumerable<object> values, string[] connectionsFields, bool addMultiple)
        {
            lock (lockObject)
            {
                var type = values.FirstOrDefault().GetType();

                if (!mainCacheDictionary.ContainsKey(TypeKey(type.Name)))
                    mainCacheDictionary.Add(TypeKey(type.Name), type);

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance(type);

                var cacheDictionaryInstance = CacheDictionaryInstance(type);

                var gValues = connectionsFields
                    .AsParallel()
                    .Select(c =>
                    {
                        return values
                            .GroupBy(g => LinkKey(c, g.GetValueFromObject(c)))
                            .Where(w => w.Key != null)
                            .Select(s =>
                            {
                                return new
                                {
                                    key = s.Key,
                                    values = s.Select(ss => ss.GetValueFromObject(fieldKey)).ToList()
                                };
                            })
                            .ToList();

                    })
                    .SelectMany(s => s)
                    .GroupBy(g => g.key)
                    .Select(s => new KeyValuePair<string, List<string>>(s.Key, s.SelectMany(ss => ss.values).ToList()))
                    .ToList();

                if (addMultiple)
                    connectionCacheDictionary.AddMultiple(gValues);
                else
                {
                    foreach (var gValue in gValues)
                        connectionCacheDictionary.Add(gValue);
                }

                gValues.Clear();

                var redisValues =
                     values
                     .AsParallel()
                     .Select(s => new KeyValuePair<string, object>(Key(s.GetValueFromObject(fieldKey), type.Name), s))
                     .ToList();

                if (addMultiple)
                    cacheDictionaryInstance.AddMultiple(redisValues);
                else
                {
                    foreach (var redisValue in redisValues)
                        cacheDictionaryInstance.Add(redisValue);
                }

                redisValues.Clear();
            }
        }

        public override void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, string[] connectionsFields, bool addMultiple)
        {
            lock (lockObject)
            {
                if (!mainCacheDictionary.ContainsKey(TypeKey<T>()))
                    mainCacheDictionary.Add(TypeKey<T>(), typeof(T));

                var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

                var cacheDictionaryInstance = CacheDictionaryInstance<T>();

                var gValues = connectionsFields
                    .AsParallel()
                    .Select(c =>
                    {
                        return values
                            .GroupBy(g => LinkKey(c, g.Value.GetValueFromObject(c)))
                            .Where(w => w.Key != null)
                            .Select(s =>
                            {
                                return new
                                {
                                    key = s.Key,
                                    values = s.Select(ss => ss.Key).ToList()
                                };
                            })
                            .ToList();

                    })
                    .SelectMany(s => s)
                    .GroupBy(g => g.key)
                    .Select(s => new KeyValuePair<string, List<string>>(s.Key, s.SelectMany(ss => ss.values).ToList()))
                    .ToList();

                if (addMultiple)
                    connectionCacheDictionary.AddMultiple(gValues);
                else
                {
                    foreach (var gValue in gValues)
                        connectionCacheDictionary.Add(gValue);
                }

                gValues.Clear();

                var redisValues = values
                     .AsParallel()
                     .Select(s => new KeyValuePair<string, T>(Key<T>(s.Key), s.Value))
                     .ToList();

                if (addMultiple)
                    cacheDictionaryInstance.AddMultiple(redisValues);
                else
                {
                    foreach (var value in redisValues)
                        cacheDictionaryInstance.Add(value);
                }

                redisValues.Clear();
            }
        }

        public override int GetAllCount<T>(ConnectionType connectionType, params ConnectionValue[] connectionValues)
        {
            return GetAllKeys<T>(connectionType, null, null, connectionValues).Count;
        }

        public override IList<string> GetAllKeys<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues)
        {
            var connectionCacheDictionary = ConnectionCacheDictionaryInstance<T>();

            if (connectionValues.Any())
            {
                if (sortFields != null
                    && sortFields.Any()
                    && connectionValues.All(a => a.SortIndex == 0))
                {
                    foreach (var connectionValue in connectionValues)
                    {
                        connectionValue.SortIndex = sortFields
                            .ToList()
                            .FindIndex(f => f == connectionValue.Field);

                        if (connectionValue.SortIndex == -1)
                            connectionValue.SortIndex = sortFields.Length + 1;
                    }
                }

                var dKeys = connectionCacheDictionary.Keys;

                var connectionValuesD = connectionValues
                    .GroupBy(g => g.Field)
                    .ToDictionary(d => d.Key, d => d.FirstOrDefault());

                if (dKeys == null)
                    return new List<string>();

                var rkeys = connectionValuesD
                    .AsParallel()
                    .Select(s =>
                    {
                        switch (s.Value.ConnectionValueType)
                        {
                            case ConnectionValueType.Contains:
                                return dKeys.Where(w => w.CaseInsensitiveContains(LinkKey(s.Value.Field, s.Value.Value)))
                                    .Select(ss => new
                                    {
                                        value = ss,
                                        sortKey = LinkKey(s.Value.SortIndex.ToString(), ss),
                                        key = s.Key,
                                    })
                                    .ToList();
                            case ConnectionValueType.StartsWith:
                                return dKeys.Where(w => w.StartsWith(LinkKey(s.Value.Field, s.Value.Value)))
                                    .Select(ss => new
                                    {
                                        value = ss,
                                        sortKey = LinkKey(s.Value.SortIndex.ToString(), ss),
                                        key = s.Key,
                                    })
                                    .ToList();
                            case ConnectionValueType.EndWith:
                                return dKeys.Where(w => w.EndsWith(LinkKey(s.Value.Field, s.Value.Value)))
                                    .Select(ss => new
                                    {
                                        value = ss,
                                        sortKey = LinkKey(s.Value.SortIndex.ToString(), ss),
                                        key = s.Key,
                                    })
                                    .ToList();
                            default:
                                return dKeys.Where(w => w.Equals(LinkKey(s.Value.Field, s.Value.Value), StringComparison.CurrentCultureIgnoreCase))
                                    .Select(ss => new
                                    {
                                        value = ss,
                                        sortKey = LinkKey(s.Value.SortIndex.ToString(), ss),
                                        key = s.Key,
                                    });
                        }

                    })
                    .SelectMany(s => s)
                    .ToList();

                var sortedrkeys = rkeys
                        .OrderBy(o => o.sortKey)
                        .Take(take.HasValue ? take.Value : rkeys.Count)
                        .GroupBy(g => g.key)
                        .ToDictionary(d => d.Key, d => d.Select(s => s.value)
                        .ToList());

                var values = sortedrkeys
                        .AsParallel()
                        .Select(s =>
                        {
                            return connectionCacheDictionary
                                .GetValues(s.Value.ToArray())
                                .SelectMany(ss => ss)
                                .Where(w => w != null).ToList();
                        })
                        .Where(w => w != null)
                        .ToList();

                var keys = values.FirstOrDefault();

                if (keys == null)
                    keys = new List<string>();

                if (connectionType == ConnectionType.And)
                {
                    foreach (var value in values)
                    {
                        keys = keys
                            .Intersect(value)
                            .ToList();
                    }
                }
                else
                {
                    keys = values
                        .SelectMany(s => s)
                        .ToList();
                }


                return keys
                    .Distinct()
                     .Take(take.HasValue ? take.Value : keys.Count)
                    .ToList();
            }

            return new List<string>();
        }

        public override IList<T> GetAllMultiple<T>()
        {
            var keys = CacheDictionaryInstance<T>().Keys;

            if (keys == null)
                return new List<T>();

            var values = CacheDictionaryInstance<T>()
                .GetValuesMultiple(keys.ToArray());

            if (values == null)
                return new List<T>();

            return values;
        }
        #endregion
    }
}
