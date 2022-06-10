// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

internal sealed class ProblemDetailsResponseMetadata
{
    /// <summary>
    /// Gets the default error type.
    /// </summary>
    public Type Type { get; } = typeof(ProblemDetails);

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> ContentTypes { get; } = new string[] { "application/problem+json" };
}
