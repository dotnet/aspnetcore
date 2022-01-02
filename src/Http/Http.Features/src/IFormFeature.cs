// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Allows reading the request body as a HTTP form.
/// </summary>
public interface IFormFeature
{
    /// <summary>
    /// Indicates if the request has a supported form content-type.
    /// </summary>
    bool HasFormContentType { get; }

    /// <summary>
    /// Gets or sets the parsed form.
    /// <para>
    /// This API will return a non-null value if the
    /// request body was read using <see cref="ReadFormAsync(CancellationToken)"/> or <see cref="ReadForm"/>, or
    /// if a value was explicitly assigned.
    /// </para>
    /// </summary>
    IFormCollection? Form { get; set; }

    /// <summary>
    /// Parses the request body as a form.
    /// <para>
    /// If the request body has not been previously read, this API performs a synchronous (blocking) read
    /// on the HTTP input stream which may be unsupported or can adversely affect application performance.
    /// Consider using <see cref="ReadFormAsync(CancellationToken)"/> instead.
    /// </para>
    /// </summary>
    /// <returns>The <see cref="IFormCollection"/>.</returns>
    IFormCollection ReadForm();

    /// <summary>
    /// Parses the request body as a form.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken);
}
