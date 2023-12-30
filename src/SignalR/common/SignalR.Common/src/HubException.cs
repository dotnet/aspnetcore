// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// The exception thrown from a hub when an error occurs.
/// </summary>
/// <remarks>
/// Exceptions often contain sensitive information, such as connection information. Because of this, SignalR does not expose the details
/// of exceptions that occur on the server to the client. However, instances of <see cref="HubException"/> <b>are</b> sent to the client.
/// </remarks>
[Serializable]
public class HubException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HubException"/> class.
    /// </summary>
    public HubException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HubException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public HubException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HubException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
    public HubException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HubException"/> class.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is <c>null</c>.</exception>
    /// <exception cref="SerializationException">The class name is <c>null</c> or <see cref="Exception.HResult"/> is zero (0).</exception>
#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#else
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
#endif
    public HubException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
