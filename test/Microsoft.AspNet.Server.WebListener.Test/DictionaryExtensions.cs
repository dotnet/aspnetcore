// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        internal static string Get(this IDictionary<string, string[]> dictionary, string key)
        {
            string[] values;
            if (dictionary.TryGetValue(key, out values))
            {
                return string.Join(", ", values);
            }
            return null;
        }

        internal static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            object values;
            if (dictionary.TryGetValue(key, out values))
            {
                return (T)values;
            }
            return default(T);
        }
    }
}
