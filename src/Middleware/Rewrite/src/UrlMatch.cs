// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

internal abstract class UrlMatch
{
    protected bool Negate { get; set; }
    public abstract MatchResults Evaluate(string input, RewriteContext context);
}
