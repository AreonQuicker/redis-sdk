using Cache.Extensions;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RedisCache.Store
{
    public class CacheDictionary<TValue> : IDictionary<string, TValue>
    {
        private readonly ConnectionMultiplexer _cnn;
        private readonly string _redisKey;

        public CacheDictionary(string host)
        {
            string redisConnection = $"{host},allowAdmin=true,SyncTimeout=30000,ConnectTimeout=10";

            ConfigurationOptions configurationOptions = ConfigurationOptions.Parse(redisConnection);
            configurationOptions.SyncTimeout = 30000;
            configurationOptions.ConnectTimeout = 10;

            _cnn = ConnectionMultiplexer.Connect(redisConnection);
        }

        public CacheDictionary(ConnectionMultiplexer connectionMultiplexer, string redisKey)
        {
            _redisKey = redisKey;
            _cnn = connectionMultiplexer;
        }

        #region Private Methods
        private IDatabase GetRedisDb()
        {
            return _cnn.GetDatabase();
        }
        #endregion

        #region Public Methods
        public void Add(string key, TValue value)
        {
            GetRedisDb()
                .HashSet(_redisKey, key, value.ObjectToJsonString());
        }

        public bool ContainsKey(string key)
        {
            return GetRedisDb()
                .HashExists(_redisKey, key);
        }

        public bool Remove(string key)
        {
            return GetRedisDb()
                .HashDelete(_redisKey, key);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            var redisValue = GetRedisDb()
                .HashGet(_redisKey, key);

            if (redisValue.IsNull)
            {
                value = default(TValue);
                return false;
            }

            value = redisValue
                .ToString()
                .JsonStringToObject<TValue>();

            return true;
        }

        public TValue GetValue(string key)
        {
            var redisValue = GetRedisDb()
                .HashGet(_redisKey, key);

            if (redisValue.IsNull)
                return default(TValue);

            return redisValue
                .ToString()
                .JsonStringToObject<TValue>();
        }

        public IList<TValue> GetValues(string[] keys)
        {
            var redisValues = keys
                .AsParallel()
                .Select(s => GetRedisDb()
                .HashGet(_redisKey, (RedisValue)s))
                .Where(w => !w.IsNull)
                .ToList();

            if (!redisValues.Any())
                return new List<TValue>();

            return redisValues
                .AsParallel()
                .Select(s => s.ToString().JsonStringToObject<TValue>())
                .Where(w => w != null)
                .ToList();
        }

        public IList<TValue> GetValuesMultiple(string[] keys)
        {
            var redisValues = GetRedisDb()
                .HashGet(_redisKey,
                keys.Select(s => (RedisValue)s).ToArray());

            if (!redisValues.Any())
                return new List<TValue>();

            return redisValues
                .AsParallel()
                .Select(s => s.ToString().JsonStringToObject<TValue>())
                .Where(w => w != null)
                .ToList();
        }

        public ICollection<TValue> Values => new Collection<TValue>(GetRedisDb().HashValues(_redisKey)
              .AsParallel()
              .Select(h => h.ToString()
              .JsonStringToObject<TValue>()).ToList());

        public ICollection<string> Keys => new Collection<string>(GetRedisDb().HashKeys(_redisKey)
              .Select(h => h.ToString()).ToList());

        public TValue this[string key]
        {
            get
            {
                var redisValue = GetRedisDb()
                    .HashGet(_redisKey, key);

                return redisValue.IsNull
                    ? default(TValue)
                    : redisValue
                    .ToString()
                    .JsonStringToObject<TValue>();
            }
            set
            {
                Add(key, value);
            }
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            GetRedisDb()
                .KeyDelete(_redisKey);
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            return GetRedisDb()
                .HashExists(_redisKey, item.Key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            GetRedisDb()
                .HashGetAll(_redisKey)
                .CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return (int)GetRedisDb().HashLength(_redisKey); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            var db = GetRedisDb();

            foreach (var hashKey in db.HashKeys(_redisKey))
            {
                var redisValue = db.HashGet(_redisKey, hashKey);
                yield return new KeyValuePair<string, TValue>(hashKey.ToString(), redisValue.ToString().JsonStringToObject<TValue>());
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            yield return GetEnumerator();
        }

        public void AddMultiple(IEnumerable<KeyValuePair<string, TValue>> items)
        {
            GetRedisDb()
                .HashSet(_redisKey, items
                .Select(i => new HashEntry(i.Key, i.Value.ObjectToJsonString()))
                .ToArray());
        }

        public void Add(IEnumerable<KeyValuePair<string, TValue>> items)
        {
            items.AsParallel()
                .ForEach(i =>
                {
                    GetRedisDb().HashSet(_redisKey, i.Key, i.Value.ObjectToJsonString());
                });
        }
        #endregion
    }
}
