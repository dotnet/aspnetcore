// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Metadata that provides endpoint-specific settings for the HttpLogging middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HttpLoggingAttribute : Attribute
{
    /// <summary>
    /// Initializes an instance of the <see cref="HttpLoggingAttribute"/> class.
    /// </summary>
    /// <param name="loggingFields">Specifies what fields to log for the endpoint.</param>
    /// <param name="requestBodyLogLimit">Specifies the maximum number of bytes to be logged for the request body. A value of <c>-1</c> means use the default setting in <see cref="HttpLoggingOptions.RequestBodyLogLimit"/>.</param>
    /// <param name="responseBodyLogLimit">Specifies the maximum number of bytes to be logged for the response body. A value of <c>-1</c> means use the default setting in <see cref="HttpLoggingOptions.ResponseBodyLogLimit"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="requestBodyLogLimit"/> or <paramref name="responseBodyLogLimit"/> is less than <c>-1</c>.</exception>
    public HttpLoggingAttribute(HttpLoggingFields loggingFields, int requestBodyLogLimit = -1, int responseBodyLogLimit = -1)
    {
        LoggingFields = loggingFields;

        ArgumentOutOfRangeException.ThrowIfLessThan(requestBodyLogLimit, -1);
        ArgumentOutOfRangeException.ThrowIfLessThan(responseBodyLogLimit, -1);

        RequestBodyLogLimit = requestBodyLogLimit;
        ResponseBodyLogLimit = responseBodyLogLimit;
    }

    /// <summary>
    /// Specifies what fields to log.
    /// </summary>
    public HttpLoggingFields LoggingFields { get; }

    /// <summary>
    /// Specifies the maximum number of bytes to be logged for the request body.
    /// </summary>
    public int RequestBodyLogLimit { get; }

    /// <summary>
    /// Specifies the maximum number of bytes to be logged for the response body.
    /// </summary>
    public int ResponseBodyLogLimit { get; }
}
