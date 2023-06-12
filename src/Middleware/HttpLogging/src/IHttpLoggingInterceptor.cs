// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Callbacks used to extend the HttpLogging middleware.
/// </summary>
public interface IHttpLoggingInterceptor
{
    /// <summary>
    /// A callback to customize the logging of the request and response.
    /// </summary>
    void OnRequest(HttpLoggingContext logContext);

    /// <summary>
    /// A callback to customize the logging of the response.
    /// </summary>
    void OnResponse(HttpLoggingContext logContext);
}
