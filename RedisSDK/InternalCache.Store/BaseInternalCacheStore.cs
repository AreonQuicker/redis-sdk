using System;
using System.Collections.Generic;
using Cache.Objects;

namespace InternalCache.Store
{

	public abstract class BaseInternalCacheStore : IInternalCacheStore
	{
		protected TimeSpan defaultCacheTimeSpan = default(TimeSpan);
		protected readonly string propertyKey;

		protected BaseInternalCacheStore(TimeSpan defaultCacheTimeSpan, string propertyKey)
		{
			this.defaultCacheTimeSpan = defaultCacheTimeSpan;
			this.propertyKey = propertyKey;
		}

		#region Protected Methods
		protected string LinkKey(params string[] names)
		{
			return string.Join("-", names);
		}
		#endregion

		#region Public Methods
		public abstract void DeleteAll<T>();
		public abstract void FlushALL();
		public abstract IList<T> Get<T>(string[] keys);
		public abstract T Get<T>(string key);
		public abstract IList<T> GetAll<T>(string eKey);
		public abstract IList<T> GetAll<T>();
		public abstract IList<T> GetAll<T>(ConnectionType connectionType, params ConnectionValue[] connectionValues);
		public abstract IList<T> GetAll<T>(ConnectionType connectionType, string eKey, params ConnectionValue[] connectionValues);
		public abstract void Set(object value);
		public abstract void Set<T>(string key, T value);
		public abstract void Set<T>(T value);
		public abstract void SetAll(IEnumerable<object> values, string eKey);
		public abstract void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, string eKey);
		public abstract void SetAll(IEnumerable<object> values);
		public abstract void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values);
		public abstract void RemoveAllExpired<T>();
		#endregion
	}

}
