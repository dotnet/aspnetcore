// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// An exception thrown when <see cref="NavigationManager.LocationChanged"/> throws an exception.
/// </summary>
public sealed class LocationChangeException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="LocationChangeException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public LocationChangeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
