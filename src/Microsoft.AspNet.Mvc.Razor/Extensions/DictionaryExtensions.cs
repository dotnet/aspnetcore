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
