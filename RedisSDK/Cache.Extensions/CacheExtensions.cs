using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Cache.Extensions
{
    public static class CacheExtensions
    {
        public static bool CaseInsensitiveContains(this string text, string value,
        StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        public static string ObjectToJsonString(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T JsonStringToObject<T>(this string serialized)
        {
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public static byte[] ObjectToByteArray(this object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }
        }

        public static T ByteArrayToObject<T>(this byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();

            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);

            var obj = (T)binForm.Deserialize(memStream);

            return obj;
        }

        public static string GetValueFromObject(this object value, string property)
        {
            var eProperty = value.GetType()?.GetProperty(property, typeof(string));

            if (eProperty != null)
                return eProperty.GetValue(value)?.ToString();

            return null;
        }

        public static bool GetValueFromObject(this object value, string property, out string propertyValue)
        {
            propertyValue = value.GetValueFromObject(property);

            if (!string.IsNullOrEmpty(propertyValue))
                return true;

            return false;
        }
    }
}
