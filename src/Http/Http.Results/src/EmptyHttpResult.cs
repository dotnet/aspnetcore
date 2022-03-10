// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// do nothing.
/// </summary>
public sealed class EmptyHttpResult : IResult
{
    internal static readonly EmptyHttpResult Instance = new();

    private EmptyHttpResult()
    {
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }
}
