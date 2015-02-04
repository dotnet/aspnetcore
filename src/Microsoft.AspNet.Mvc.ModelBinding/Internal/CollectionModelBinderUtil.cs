// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class CollectionModelBinderUtil
    {
        public static IEnumerable<string> GetIndexNamesFromValueProviderResult(ValueProviderResult valueProviderResult)
        {
            IEnumerable<string> indexNames = null;
            if (valueProviderResult != null)
            {
                var indexes = (string[])valueProviderResult.ConvertTo(typeof(string[]));
                if (indexes != null && indexes.Length > 0)
                {
                    indexNames = indexes;
                }
            }

            return indexNames;
        }
    }
}
