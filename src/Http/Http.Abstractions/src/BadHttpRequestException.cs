// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an HTTP request error
/// </summary>
public class BadHttpRequestException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadHttpRequestException"/> class.
    /// </summary>
    /// <param name="message">The message to associate with this exception.</param>
    /// <param name="statusCode">The HTTP status code to associate with this exception.</param>
    public BadHttpRequestException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadHttpRequestException"/> class with the <see cref="StatusCode"/> set to 400 Bad Request.
    /// </summary>
    /// <param name="message">The message to associate with this exception</param>
    public BadHttpRequestException(string message)
        : base(message)
    {
        StatusCode = StatusCodes.Status400BadRequest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadHttpRequestException"/> class.
    /// </summary>
    /// <param name="message">The message to associate with this exception.</param>
    /// <param name="statusCode">The HTTP status code to associate with this exception.</param>
    /// <param name="innerException">The inner exception to associate with this exception</param>
    public BadHttpRequestException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadHttpRequestException"/> class with the <see cref="StatusCode"/> set to 400 Bad Request.
    /// </summary>
    /// <param name="message">The message to associate with this exception</param>
    /// <param name="innerException">The inner exception to associate with this exception</param>
    public BadHttpRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = StatusCodes.Status400BadRequest;
    }

    /// <summary>
    /// Gets the HTTP status code for this exception.
    /// </summary>
    public int StatusCode { get; }
}
