// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCachingFeature : IResponseCachingFeature
    {
        private string[] _varyByQueryKeys;

        public string[] VaryByQueryKeys
        {
            get
            {
                return _varyByQueryKeys;
            }
            set
            {
                if (value?.Length > 1)
                {
                    for (var i = 0; i < value.Length; i++)
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
