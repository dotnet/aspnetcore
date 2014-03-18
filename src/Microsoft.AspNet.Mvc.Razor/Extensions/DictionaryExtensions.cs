using Microsoft.AspNet.Mvc;

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static T GetValueOrDefault<T>([NotNull] this IDictionary<string, object> dictionary, [NotNull] string key)
        {
            object valueAsObject;
            if (dictionary.TryGetValue(key, out valueAsObject))
            {
                if (valueAsObject is T)
                {
                    return (T)valueAsObject;
                }
            }

            return default(T);
        }
    }
}
