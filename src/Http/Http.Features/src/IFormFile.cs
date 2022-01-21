// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents a file sent with the HttpRequest.
/// </summary>
public interface IFormFile
{
    /// <summary>
    /// Gets the raw Content-Type header of the uploaded file.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the raw Content-Disposition header of the uploaded file.
    /// </summary>
    string ContentDisposition { get; }

    /// <summary>
    /// Gets the header dictionary of the uploaded file.
    /// </summary>
    IHeaderDictionary Headers { get; }

    /// <summary>
    /// Gets the file length in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Gets the form field name from the Content-Disposition header.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the file name from the Content-Disposition header.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Opens the request stream for reading the uploaded file.
    /// </summary>
    Stream OpenReadStream();

    /// <summary>
    /// Copies the contents of the uploaded file to the <paramref name="target"/> stream.
    /// </summary>
    /// <param name="target">The stream to copy the file contents to.</param>
    void CopyTo(Stream target);

    /// <summary>
    /// Asynchronously copies the contents of the uploaded file to the <paramref name="target"/> stream.
    /// </summary>
    /// <param name="target">The stream to copy the file contents to.</param>
    /// <param name="cancellationToken"></param>
    Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken));
}
