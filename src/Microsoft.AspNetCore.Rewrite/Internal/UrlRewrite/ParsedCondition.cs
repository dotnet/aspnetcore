// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class ParsedCondition
    {
        public bool Negate { get; set; }
        public bool IgnoreCase { get; set; } = true;
        public MatchType MatchType { get; set; } = MatchType.Pattern;
    }
}
