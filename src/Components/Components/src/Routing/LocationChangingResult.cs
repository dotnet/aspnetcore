// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Represents the result of a location change.
/// </summary>
public class LocationChangingResult
{
    /// <summary>
    /// Gets whether the location change was canceled by a handler.
    /// </summary>
    public bool Canceled { get; }

    /// <summary>
    /// Gets any execeptions thrown by handlers.
    /// </summary>
    public AggregateException? Exception { get; }

    internal LocationChangingResult(bool canceled, AggregateException? exception)
    {
        Canceled = canceled;
        Exception = exception;
    }
}
