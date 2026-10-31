// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// An interface which can be used to identify a type which provides metadata needed for enabling CORS support.
/// </summary>
public interface IEnableCorsAttribute : ICorsMetadata
{
    /// <summary>
    /// The name of the policy which needs to be applied.
    /// </summary>
    string? PolicyName { get; set; }
}
