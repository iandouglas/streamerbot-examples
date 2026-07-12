using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iandouglas736
{
    /// <summary>
    /// Data conversion helpers for Streamer.bot actions.
    /// No Streamer.bot context is required.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Parses a JSON string into a nested Dictionary<string, object>.
        /// JSON objects become dictionaries, arrays become List<object>, and primitive
        /// values become their natural C# types (string, int, long, double, bool, DateTime, null).
        /// 
        /// This is useful when you want to consume an API response or JSON file without defining
        /// typed classes for every possible structure.
        /// </summary>
        public static Dictionary<string, object> JsonToNestedDictionary(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object>();

            JObject obj = JsonConvert.DeserializeObject<JObject>(json);
            if (obj == null)
                return new Dictionary<string, object>();

            return (Dictionary<string, object>)ConvertJToken(obj);
        }

        /// <summary>
        /// Parses a JSON array string into a List<object> with inferred element types.
        /// </summary>
        public static List<object> JsonToNestedList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<object>();

            JArray arr = JsonConvert.DeserializeObject<JArray>(json);
            if (arr == null)
                return new List<object>();

            return (List<object>)ConvertJToken(arr);
        }

        /// <summary>
        /// Reads a JSON file from disk and converts it to a nested dictionary.
        /// </summary>
        public static Dictionary<string, object> JsonFileToNestedDictionary(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return new Dictionary<string, object>();

            string json = System.IO.File.ReadAllText(filePath);
            return JsonToNestedDictionary(json);
        }

        /// <summary>
        /// Converts a JSON object/array into a plain .NET object graph with inferred types.
        /// </summary>
        private static object ConvertJToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var property in (JObject)token)
                    {
                        dict[property.Key] = ConvertJToken(property.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                    {
                        list.Add(ConvertJToken(item));
                    }
                    return list;

                case JTokenType.Integer:
                    return token.ToObject<long>();

                case JTokenType.Float:
                    return token.ToObject<double>();

                case JTokenType.Boolean:
                    return token.ToObject<bool>();

                case JTokenType.Date:
                    return token.ToObject<DateTime>();

                case JTokenType.String:
                default:
                    return token.ToObject<string>();
            }
        }

        /// <summary>
        /// Safely gets a nested value from a dictionary created by JsonToNestedDictionary.
        /// Supports dot-separated paths like "data.user.name".
        /// Returns null if any part of the path is missing.
        /// </summary>
        public static object GetValue(Dictionary<string, object> dictionary, string path)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(path))
                return null;

            string[] parts = path.Split('.');
            object current = dictionary;

            foreach (string part in parts)
            {
                if (current is Dictionary<string, object> dict && dict.TryGetValue(part, out object next))
                {
                    current = next;
                }
                else if (current is List<object> list && int.TryParse(part, out int index) && index >= 0 && index < list.Count)
                {
                    current = list[index];
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// Safely gets a typed nested value from a dictionary created by JsonToNestedDictionary.
        /// Returns default(T) if the path is missing or the value cannot be converted.
        /// </summary>
        public static T GetValue<T>(Dictionary<string, object> dictionary, string path)
        {
            object value = GetValue(dictionary, path);
            if (value == null)
                return default(T);

            try
            {
                if (value is T typedValue)
                    return typedValue;

                if (typeof(T).IsEnum && value is string strValue)
                {
                    try
                    {
                        object parsed = Enum.Parse(typeof(T), strValue, true);
                        return (T)parsed;
                    }
                    catch { }
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Serializes any object to a JSON string. Useful for storing dictionaries in
        /// Streamer.bot global variables without cross-AppDomain serialization issues.
        /// </summary>
        public static string ToJson(object value)
        {
            if (value == null)
                return null;
            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Deserializes a JSON string to the specified type.
        /// Returns default(T) if the input is null/empty or invalid.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
