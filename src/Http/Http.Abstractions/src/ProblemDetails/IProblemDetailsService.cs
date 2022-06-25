// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// 
/// </summary>
public interface IProblemDetailsService
{
    bool IsEnabled(ProblemTypes type);

    Task WriteAsync(
        HttpContext context,
        EndpointMetadataCollection? additionalMetadata = null,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null);
}
