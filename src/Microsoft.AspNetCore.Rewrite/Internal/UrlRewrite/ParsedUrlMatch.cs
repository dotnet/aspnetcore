// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class ParsedUrlMatch
    {
        public bool IgnoreCase { get; set; }
        public bool Negate { get; set; }
    }
}
