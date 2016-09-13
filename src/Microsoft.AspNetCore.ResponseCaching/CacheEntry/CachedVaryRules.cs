// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class CachedVaryRules
    {
        public string VaryKeyPrefix { get; internal set; }

        public StringValues Headers { get; internal set; }

        public StringValues Params { get; internal set; }
    }
}
