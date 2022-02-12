// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// do nothing.
/// </summary>
internal sealed class EmptyResult : IResult
{
    private static readonly Task CompletedTask = Task.CompletedTask;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        return CompletedTask;
    }
}
