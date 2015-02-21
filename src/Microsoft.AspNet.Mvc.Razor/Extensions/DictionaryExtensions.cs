// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Internal;

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static T GetValueOrDefault<T>([NotNull] this IDictionary<string, object> dictionary,
                                             [NotNull] string key)
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
