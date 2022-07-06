// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract used to specify metadata,in <see cref="Endpoint.Metadata"/>,
/// for configure <see cref=" Mvc.ProblemDetails"/> generation.
/// </summary>
public interface IProblemDetailsMetadata
{
    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    int? StatusCode { get; }

    /// <summary>
    /// Gets the Problem Details Types
    /// associated to the <see cref="StatusCode"/>.
    /// </summary>
    ProblemDetailsTypes ProblemType { get; }
}
