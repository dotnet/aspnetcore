// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// do nothing.
/// </summary>
public sealed class Empty : IResult
{
    private Empty()
    {
    }

    /// <summary>
    /// Gets an instance of <see cref="Empty"/>.
    /// </summary>
    public static Empty Instance { get; } = new();

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }
}
