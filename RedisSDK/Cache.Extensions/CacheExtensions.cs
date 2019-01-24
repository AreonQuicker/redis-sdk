using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static IEnumerable<T> MultipleSort<T>(this IEnumerable<T> data, params string[] sortBy)
        {
            return data.MultipleSort<T>(sortBy.Select(s => new Tuple<string, string>(s, "asc")).ToList());
        }

        public static IEnumerable<T> MultipleSort<T>(this IEnumerable<T> collection, List<Tuple<string, string>> sortBy)
        {
            var sortExpressions = new List<Tuple<string,
                string>>();

            for (int i = 0; i < sortBy.Count; i++)
            {
                var fieldName = sortBy[i].Item1.Trim();

                var sortOrder = (sortBy[i].Item2 != null && sortBy[i].Item2.Length > 1) ?
                    sortBy[i].Item2.Trim().ToLower() : "asc";

                sortExpressions.Add(new Tuple<string, string>(fieldName, sortOrder));
            }
            if ((sortExpressions == null) || (sortExpressions.Count <= 0))
            {
                return collection;
            }

            IEnumerable<T> query = from item in collection select item;
            IOrderedEnumerable<T> orderedQuery = null;

            for (int i = 0; i < sortExpressions.Count; i++)
            {
                var index = i;

                Func<T, object> expression = item => item.GetType()
                 .GetProperty(sortExpressions[index].Item1)
                 .GetValue(item, null);

                if (sortExpressions[index].Item2 == "asc")
                {
                    orderedQuery = (index == 0) ? query.OrderBy(expression) :
                        orderedQuery.ThenBy(expression);
                }
                else
                {
                    orderedQuery = (index == 0) ? query.OrderByDescending(expression) :
                        orderedQuery.ThenByDescending(expression);
                }
            }
            query = orderedQuery;

            return query;
        }

        public static IEnumerable<T> Sort<T>(this IEnumerable<T> collection, params string[] sortBy)
        {
            return collection
                .MultipleSort(sortBy);
        }

        public static IEnumerable<T> SortAndTake<T>(this IEnumerable<T> collection, int? take, params string[] sortBy)
        {
            if (take.HasValue)
            {
                return collection
                    .Sort(sortBy)
                    .Take(take.Value)
                    .ToList();
            }
            else
            {
                return collection
                   .Sort(sortBy)
                   .ToList();
            }
        }
    }
}
