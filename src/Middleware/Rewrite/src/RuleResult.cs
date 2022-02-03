// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite;

/// <summary>
/// An enum representing the result of a rule.
/// </summary>
public enum RuleResult
{
    /// <summary>
    /// Default value, continue applying rules.
    /// </summary>
    ContinueRules,
    ///<summary>
    /// The rule ended the request by providing a response.
    /// </summary>
    EndResponse,
    /// <summary>
    /// Stop applying rules and send context to the next middleware
    /// </summary>
    SkipRemainingRules
}
