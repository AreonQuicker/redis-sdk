using Cache.Objects;
using System.Collections.Generic;

namespace RedisCache.SDK
{
    public interface ICacheSDK
    {
        void InitCache(string host, int? port);

        void SetInternal(object value);
        void SetInternal<T>(string key, T value);
        void SetInternal<T>(T value);
        void SetInternalAll(IEnumerable<object> values);
        void SetInternalAll<T>(IEnumerable<KeyValuePair<string, T>> values);

        void Set(object value);
        void Set<T>(string key, T value);
        void Set<T>(T value);
        void SetAll(IEnumerable<object> values, bool addMultiple);
        void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple);

        IList<T> GetAll<T>();
        IList<T> GetAllMultiple<T>();
        IList<T> GetAll<T>(ConnectionType connectionType, string[] sortFields, int? take, params ConnectionValue[] connectionValues);
        IList<T> Get<T>(string[] keys);
        T Get<T>(string key);

        void DeleteAllInternal<T>();

        void FlushALLInternal();
    }

}
