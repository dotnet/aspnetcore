// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// An exception that is thrown when a connection is aborted by the server.
/// </summary>
public class ConnectionAbortedException : OperationCanceledException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    public ConnectionAbortedException() :
        this("The connection was aborted")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ConnectionAbortedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionAbortedException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The underlying <see cref="Exception"/>.</param>
    public ConnectionAbortedException(string message, Exception inner) : base(message, inner)
    {
    }
}
