using System;
using System.Collections.Generic;
using Cache.Objects;

namespace RedisCache.Store
{
	public interface ICacheStore
	{
		TimeSpan DefaultCacheTimeSpan { get; }

		void Init();
		void Init(string host);
		void Init(string host, int? port);

		void Set(object value);
		void Set<T>(string key, T value);

		void SetAll(IEnumerable<object> values, bool addMultiple);
		void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple);

		void Set(object value, string[] connectionsFields);
		void Set<T>(string key, T value, string[] connectionsFields);

		void SetAll(IEnumerable<object> values, string[] connectionsFields, bool addMultiple);
		void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, string[] connectionsFields, bool addMultiple);

		IList<T> GetAll<T>();
		IList<T> GetAllMultiple<T>();
		IList<T> GetAll<T>(ConnectionType connectionType, params ConnectionValue[] connectionValues);
		int GetAllCount<T>(ConnectionType connectionType, params ConnectionValue[] connectionValues);
		IList<string> GetAllKeys<T>(ConnectionType connectionType, params ConnectionValue[] connectionValues);

		IList<T> Get<T>(string[] keys);
		T Get<T>(string key);

		IList<Type> GetAllTypes();

		void FlushALL();
		void Remove<T>(string key);
		void Remove<T>(string[] keys);
		void Remove(string[] keys, Type type);

		void DeleteAll<T>();
		void DeleteAll(Type type);
	}
}
