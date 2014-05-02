// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
