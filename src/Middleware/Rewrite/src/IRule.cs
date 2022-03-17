// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// Represents a rule.
/// </summary>
public interface IRule
{
    /// <summary>
    /// Applies the rule.
    /// Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result"/>
    /// (defaults to RuleResult.ContinueRules)
    /// </summary>
    /// <param name="context"></param>
    void ApplyRule(RewriteContext context);
}

