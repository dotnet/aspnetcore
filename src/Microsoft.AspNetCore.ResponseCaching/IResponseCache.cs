// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IResponseCache
    {
        object Get(string key);
        // TODO: Set expiry policy in the underlying cache?
        void Set(string key, object entry);
        void Remove(string key);
    }
}
