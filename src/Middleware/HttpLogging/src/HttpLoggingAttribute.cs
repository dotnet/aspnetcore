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
    public HttpLoggingAttribute(HttpLoggingFields loggingFields)
    {
        LoggingFields = loggingFields;
    }

    private int _responseBodyLogLimit;
    private int _requestBodyLogLimit;

    /// <summary>
    /// Specifies what fields to log.
    /// </summary>
    public HttpLoggingFields LoggingFields { get; }

    /// <summary>
    /// Indicates whether <see cref="RequestBodyLogLimit"/> has been set.
    /// </summary>
    public bool IsRequestBodyLogLimitSet { get; private set; }

    /// <summary>
    /// Specifies the maximum number of bytes to be logged for the request body.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="RequestBodyLogLimit"/> set to a value less than <c>0</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when getting <see cref="RequestBodyLogLimit"/> if it hasn't been set to a value. Check <see cref="IsRequestBodyLogLimitSet"/> first.</exception>
    public int RequestBodyLogLimit
    {
        get
        {
            if (IsRequestBodyLogLimitSet)
            {
                return _requestBodyLogLimit;
            }

            throw new InvalidOperationException($"{nameof(RequestBodyLogLimit)} was not set. Check {nameof(IsRequestBodyLogLimitSet)} before accessing this property.");
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RequestBodyLogLimit));
            _requestBodyLogLimit = value;
            IsRequestBodyLogLimitSet = true;
        }
    }

    /// <summary>
    /// Indicates whether <see cref="ResponseBodyLogLimit"/> has been set.
    /// </summary>
    public bool IsResponseBodyLogLimitSet { get; private set; }

    /// <summary>
    /// Specifies the maximum number of bytes to be logged for the response body.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="ResponseBodyLogLimit"/> set to a value less than <c>0</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when getting <see cref="ResponseBodyLogLimit"/> if it hasn't been set to a value. Check <see cref="IsResponseBodyLogLimitSet"/> first.</exception>
    public int ResponseBodyLogLimit
    {
        get
        {
            if (IsResponseBodyLogLimitSet)
            {
                return _responseBodyLogLimit;
            }
            throw new InvalidOperationException($"{nameof(ResponseBodyLogLimit)} was not set. Check {nameof(IsResponseBodyLogLimitSet)} before accessing this property.");
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ResponseBodyLogLimit));
            _responseBodyLogLimit = value;
            IsResponseBodyLogLimitSet = true;
        }
    }
}
