// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    internal abstract class UrlMatch
    {
        protected bool Negate { get; set; }
        public abstract MatchResults Evaluate(string input, RewriteContext context);
    }
}
