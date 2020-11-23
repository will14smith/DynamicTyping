using System.Collections.Generic;

namespace DynamicTyping.Actual
{
    public interface IReadOnlyProperties<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
       bool TryGetValue(TKey name, out TValue value);
       IProperties<TKey, TValue> Copy();
    }
    
    public interface IProperties<TKey, TValue> : IReadOnlyProperties<TKey, TValue>
    {
       bool TrySetValue<T>(TKey name, T value); 
    }

    public interface IAssetProperties
    {
        string AssetName { get; set; }
        string AssetId { get; set; }
        string OriginalPortfolioName {get; set;}
        string Path { get; set; }
        decimal? RootQuantity { get; set; }
    }

    public static class PropertyExtensions
    {
        public static bool TryGetValue<T>(this IReadOnlyProperties<string, object> properties, string name, out T value)
        {
            var exists = properties.TryGetValue(name, out var result);
            value = exists && result != null ? (T) result : default;
            return exists;
        }
        
        public static TResult SafeGet<TResult>(this IReadOnlyProperties<string, object> properties, string name)
        {
            var exists = properties.TryGetValue<TResult>(name, out var value);
            return exists ? value : default;
        }
        
        public static TResult Get<TResult>(this IReadOnlyProperties<string, object> properties, string name)
        {
            var exists = properties.TryGetValue<TResult>(name, out var value);
            return exists ? value : throw new KeyNotFoundException(name);
        }

        public static object Get(this IReadOnlyProperties<string, object> properties, string name)
        {
            var exists = properties.TryGetValue(name, out var value);
            return exists ? value : throw new KeyNotFoundException(name);
        }

        public static void Set<TValue>(this IProperties<string, TValue> properties, string name, TValue value)
        {
            var result = properties.TrySetValue(name, value);
            if (!result)
            {
                throw new KeyNotFoundException(name);
            }
        }
        
        public static bool ContainsKey(this IReadOnlyProperties<string, object> properties, string name) => properties.TryGetValue<object>(name, out _);
    }
}