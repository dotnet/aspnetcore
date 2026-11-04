// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// An exception which indicates multiple matches in action selection.
/// </summary>
[Serializable]
public class AmbiguousActionException : InvalidOperationException
{
    /// <summary>
    /// Creates a new instance of <see cref="AmbiguousActionException" />.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AmbiguousActionException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Framework infrastructure. Do not call directly.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected AmbiguousActionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
