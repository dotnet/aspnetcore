// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        internal static void Append(this IDictionary<string, string[]> dictionary, string key, string value)
        {
            string[] orriginalValues;
            if (dictionary.TryGetValue(key, out orriginalValues))
            {
                string[] newValues = new string[orriginalValues.Length + 1];
                orriginalValues.CopyTo(newValues, 0);
                newValues[newValues.Length - 1] = value;
                dictionary[key] = newValues;
            }
            else
            {
                dictionary[key] = new string[] { value };
            }
        }

        internal static string Get(this IDictionary<string, string[]> dictionary, string key)
        {
            string[] values;
            if (dictionary.TryGetValue(key, out values))
            {
                return string.Join(", ", values);
            }
            return null;
        }

        internal static T Get<T>(this IDictionary<string, object> dictionary, string key, T fallback = default(T))
        {
            object values;
            if (dictionary.TryGetValue(key, out values))
            {
                return (T)values;
            }
            return fallback;
        }
    }
}
