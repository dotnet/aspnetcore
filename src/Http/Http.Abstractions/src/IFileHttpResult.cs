// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the file result of an HTTP result endpoint.
/// </summary>
public interface IFileHttpResult : IResult
{
    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the file name that will be used in the Content-Disposition header of the response.
    /// </summary>
    string? FileDownloadName { get; }

    /// <summary>
    /// Gets or sets the last modified information associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    DateTimeOffset? LastModified { get; }

    /// <summary>
    /// Gets or sets the etag associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    EntityTagHeaderValue? EntityTag { get; }

    /// <summary>
    /// Gets or sets the value that enables range processing for the <see cref="IFileHttpResult"/>.
    /// </summary>
    bool EnableRangeProcessing { get; }

    /// <summary>
    /// Gets or sets the file length information associated with the <see cref="IFileHttpResult"/>.
    /// </summary>
    long? FileLength { get; }
}
