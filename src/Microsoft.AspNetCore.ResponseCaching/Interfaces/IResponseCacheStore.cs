// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IResponseCacheStore
    {
        object Get(string key);
        void Set(string key, object entry, TimeSpan validFor);
        void Remove(string key);
    }
}
