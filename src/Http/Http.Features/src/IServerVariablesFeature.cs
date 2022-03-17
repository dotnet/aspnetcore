// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// This feature provides access to request server variables set.
/// </summary>
public interface IServerVariablesFeature
{
    /// <summary>
    /// Gets or sets the value of a server variable for the current request.
    /// </summary>
    /// <param name="variableName">The variable name</param>
    /// <returns>May return null or empty if the variable does not exist or is not set.</returns>
    string? this[string variableName] { get; set; }
}
