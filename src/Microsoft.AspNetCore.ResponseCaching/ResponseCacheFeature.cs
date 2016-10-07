// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCacheFeature : IResponseCacheFeature
    {
        private StringValues _varyByQueryKeys;

        public StringValues VaryByQueryKeys
        {
            get
            {
                return _varyByQueryKeys;
            }
            set
            {
                if (value.Count > 1)
                {
                    for (var i = 0; i < value.Count; i++)
                    {
                        if (string.IsNullOrEmpty(value[i]))
                        {
                            throw new ArgumentException($"When {nameof(value)} contains more than one value, it cannot contain a null or empty value.", nameof(value));
                        }
                    }
                }
                _varyByQueryKeys = value;
            }
        }
    }
}
