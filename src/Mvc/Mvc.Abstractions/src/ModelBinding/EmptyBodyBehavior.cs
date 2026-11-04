// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Determines the behavior for processing empty bodies during input formatting.
/// </summary>
public enum EmptyBodyBehavior
{
    /// <summary>
    /// Uses the framework default behavior for processing empty bodies.
    /// This is typically configured using <c>MvcOptions.AllowEmptyInputInBodyModelBinding</c>.
    /// </summary>
    Default,

    /// <summary>
    /// Empty bodies are treated as valid inputs.
    /// </summary>
    Allow,

    /// <summary>
    /// Empty bodies are treated as invalid inputs.
    /// </summary>
    Disallow,
}
