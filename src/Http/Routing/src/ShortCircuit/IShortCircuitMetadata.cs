// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Short circuit the endpoint(s).
/// The execution of the endpoint will happen in UseRouting middleware.
/// This prevents other middleware from running, including authorization middleware and its fallback policy.
/// </summary>
public interface IShortCircuitMetadata
{
    /// <summary>
    /// The status code to set in the response.
    /// </summary>
    int? StatusCode { get; }
}
