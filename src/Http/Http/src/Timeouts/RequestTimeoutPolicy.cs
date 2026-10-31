// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

/// <summary>
/// Defines the policy for request timeouts middleware.
/// </summary>
public sealed class RequestTimeoutPolicy
{
    /// <summary>
    /// The timeout to apply.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Status code to be set in response when a timeout results in an <see cref="OperationCanceledException" /> being caught by the middleware.
    /// The status code cannot be applied if the response has already started.
    /// 504 will be used if none is specified.
    /// </summary>
    public int? TimeoutStatusCode { get; init; }

    /// <summary>
    /// A callback for creating a timeout response.
    /// This is called if a timeout results in an <see cref="OperationCanceledException" /> being caught by the middleware.
    /// The status code will be set first.
    /// The status code and callback cannot be applied if the response has already started.
    /// The default behavior is an empty response with only the status code.
    /// </summary>
    public RequestDelegate? WriteTimeoutResponse { get; init; }
}
