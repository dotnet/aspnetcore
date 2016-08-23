// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    public enum OverrideResult
    {
        /// <summary>
        /// Use the default logic for determining cacheability.
        /// </summary>
        UseDefaultLogic,

        /// <summary>
        /// Ignore default logic and do not cache.
        /// </summary>
        DoNotCache,

        /// <summary>
        /// Ignore default logic and cache.
        /// </summary>
        Cache
    }
}