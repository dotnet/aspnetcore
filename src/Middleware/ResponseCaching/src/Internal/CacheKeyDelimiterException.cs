// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal sealed class CacheKeyDelimiterException : Exception
    {
        public CacheKeyDelimiterException()
            : base("The value contains invalid characters to cache.")
        {
        }
    }
}
