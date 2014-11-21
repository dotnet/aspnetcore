// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace RoutingSample.Web
{
    public static class DictionaryExtensions
    {
        public static string Print(this IDictionary<string, object> routeValues)
        {
            var values = routeValues.Select(kvp => kvp.Key + ":" + kvp.Value.ToString());

            return string.Join(" ", values);
        }
    }
}