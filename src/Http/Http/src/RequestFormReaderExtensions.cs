// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension for <see cref="HttpRequest"/>.
/// </summary>
public static class RequestFormReaderExtensions
{
    /// <summary>
    /// Read the request body as a form with the given options. These options will only be used
    /// if the form has not already been read.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="options">Options for reading the form.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The parsed form.</returns>
    public static Task<IFormCollection> ReadFormAsync(this HttpRequest request, FormOptions options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        if (!request.HasFormContentType)
        {
            throw new InvalidOperationException("Incorrect Content-Type: " + request.ContentType);
        }

        var features = request.HttpContext.Features;
        var formFeature = features.Get<IFormFeature>();
        if (formFeature == null || formFeature.Form == null)
        {
            // We haven't read the form yet, replace the reader with one using our own options.
            features.Set<IFormFeature>(new FormFeature(request, options));
        }
        return request.ReadFormAsync(cancellationToken);
    }
}
