// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// An exception that is thrown when there the current address Kestrel is trying to bind to is in use.
/// </summary>
public class AddressInUseException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of <see cref="AddressInUseException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AddressInUseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AddressInUseException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The underlying <see cref="Exception"/>.</param>
    public AddressInUseException(string message, Exception inner) : base(message, inner)
    {
    }
}
