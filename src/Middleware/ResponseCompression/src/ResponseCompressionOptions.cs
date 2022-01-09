// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// Options for the HTTP response compression middleware.
/// </summary>
public class ResponseCompressionOptions
{
    /// <summary>
    /// Response Content-Type MIME types to compress.
    /// </summary>
    public IEnumerable<string> MimeTypes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Response Content-Type MIME types to not compress.
    /// </summary>
    public IEnumerable<string> ExcludedMimeTypes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Indicates if responses over HTTPS connections should be compressed. The default is 'false'.
    /// Enabling compression on HTTPS requests for remotely manipulable content may expose security problems.
    /// </summary>
    /// <remarks>
    /// This can be overridden per request using <see cref="IHttpsCompressionFeature"/>.
    /// </remarks>
    public bool EnableForHttps { get; set; }

    /// <summary>
    /// The <see cref="ICompressionProvider"/> types to use for responses.
    /// Providers are prioritized based on the order they are added.
    /// </summary>
    public CompressionProviderCollection Providers { get; } = new CompressionProviderCollection();
}
