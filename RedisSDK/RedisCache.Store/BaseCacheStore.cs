using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace RedisCache.Store
{
	public abstract class BaseCacheStore : ICacheStore
	{
		#region Private Readonly Variables
		private readonly Dictionary<ConfigurationType, object> _configurations = null;
		#endregion

		#region Protected Variables
		protected string appKey;
		protected string fieldKey;
		protected ConnectionMultiplexer connectionMultiplexer;
		protected IDatabase database => connectionMultiplexer.GetDatabase();
		protected bool initialized = false;
		protected object lockObject = new object();
		protected TimeSpan defaultCacheTimeSpan;
		protected string redisServerConnection;
		#endregion

		#region Key Builders
		#endregion

		#region Public Properties     
		public TimeSpan DefaultCacheTimeSpan => defaultCacheTimeSpan;
		#endregion

		protected BaseCacheStore(Dictionary<ConfigurationType, object> configurations, bool init = false)
		{
			_configurations = configurations;

			if(_configurations.ContainsKey(ConfigurationType.DefaultCacheTimeSpan))
				defaultCacheTimeSpan = (TimeSpan)_configurations[ConfigurationType.DefaultCacheTimeSpan];
			else
				defaultCacheTimeSpan = new TimeSpan(0, 10, 0);

			if(_configurations.ContainsKey(ConfigurationType.AppKey))
				appKey = (string)_configurations[ConfigurationType.AppKey];
			else
				appKey = Guid.NewGuid().ToString();

			if(_configurations.ContainsKey(ConfigurationType.Key))
				fieldKey = (string)_configurations[ConfigurationType.Key];
			else
				fieldKey = "CacheKey";

			if(init)
			{
				if(configurations.ContainsKey(ConfigurationType.Port))
					Init((string)configurations[ConfigurationType.Host], (int?)configurations[ConfigurationType.Host]);
				else
					Init((string)configurations[ConfigurationType.Host]);
			}
		}

		#region Protected Methods  
		protected string AppKey { get { return (string.IsNullOrWhiteSpace(appKey) ? "" : $"{appKey}."); } }
		protected string TypeKey<T>()
		{
			return $"Type::{typeof(T).Name}";
		}
		protected string TypeKey(string name)
		{
			return $"Type::{name}";
		}
		protected string Key<T>(string key)
		{
			return $"Key::{typeof(T).Name}.{key}";
		}
		protected string Key(string key, string name)
		{
			return $"Key::{name}.{key}";
		}
		protected string ConnectionTypeKey<T>()
		{
			return $"ConnectionType::{AppKey}{typeof(T).Name}";
		}
		protected string ConnectionTypeKey(string name)
		{
			return $"ConnectionType::{AppKey}{name}";
		}
		protected string LinkKey(params string[] names)
		{
			if(names.Any(a => a == null))
				return null;

			return string.Join("-", names);
		}
		#endregion

		#region Public Methods
		public void Init(ConnectionMultiplexer connectionMultiplexer)
		{
			this.connectionMultiplexer = connectionMultiplexer;

			initialized = true;
		}
		public void Init(string host, int? port)
		{
			if(string.IsNullOrEmpty(host)
				&& _configurations.ContainsKey(ConfigurationType.Host))
				host = (string)_configurations[ConfigurationType.Host];

			string redisConnection = port.HasValue
				? $"{host}:{port.Value},allowAdmin=true,SyncTimeout=30000,ConnectTimeout=100"
				: $"{host},allowAdmin=true,SyncTimeout=30000,ConnectTimeout=100";

			redisServerConnection = port.HasValue
				? $"{host}:{port.Value}"
				: $"{host}";

			connectionMultiplexer = ConnectionMultiplexer.Connect(redisServerConnection);

			initialized = true;
		}
		public void Init(string host)
		{
			Init(host, null);
		}
		public void Init()
		{
			Init(null, null);
		}
		#endregion

		#region Abstract Methods
		public abstract void Set(object value);
		public abstract void Set<T>(string key, T value);
		public abstract void SetAll(IEnumerable<object> values, bool addMultiple);
		public abstract void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, bool addMultiple);
		public abstract void Set(object value, string[] connectionsFields);
		public abstract void Set<T>(string key, T value, string[] connectionsFields);
		public abstract void SetAll(IEnumerable<object> values, string[] connectionsFields, bool addMultiple);
		public abstract void SetAll<T>(IEnumerable<KeyValuePair<string, T>> values, string[] connectionsFields, bool addMultiple);
		public abstract IList<T> GetAll<T>();
		public abstract IList<T> GetAll<T>(Cache.Objects.ConnectionType connectionType, params Cache.Objects.ConnectionValue[] connectionValues);
		public abstract IList<T> Get<T>(string[] keys);
		public abstract T Get<T>(string key);
		public abstract IList<Type> GetAllTypes();
		public abstract void FlushALL();
		public abstract void Remove<T>(string key);
		public abstract void Remove<T>(string[] keys);
		public abstract void Remove(string[] keys, Type type);
		public abstract void DeleteAll<T>();
		public abstract void DeleteAll(Type type);
		public abstract int GetAllCount<T>(Cache.Objects.ConnectionType connectionType, params Cache.Objects.ConnectionValue[] connectionValues);
		public abstract IList<string> GetAllKeys<T>(Cache.Objects.ConnectionType connectionType, params Cache.Objects.ConnectionValue[] connectionValues);
		public abstract IList<T> GetAllMultiple<T>();
		#endregion
	}
}
