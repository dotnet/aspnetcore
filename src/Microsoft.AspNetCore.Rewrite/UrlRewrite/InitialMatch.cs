// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite
{
    public class InitialMatch
    {
        public Regex Url { get; set; } // TODO must be a non-empty string, throw in check after parsing?
        public bool IgnoreCase { get; set; } = true;
        public bool Negate { get; set; }
    }
}
