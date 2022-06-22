// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents a cached response body.
/// </summary>
internal sealed class CachedResponseBody
{
    /// <summary>
    /// Creates a new <see cref="CachedResponseBody"/> instance.
    /// </summary>
    /// <param name="segments">The segments.</param>
    /// <param name="length">The length.</param>
    public CachedResponseBody(List<byte[]> segments, long length)
    {
        ArgumentNullException.ThrowIfNull(segments);

        Segments = segments;
        Length = length;
    }

    /// <summary>
    /// Gets the segments of the body.
    /// </summary>
    public List<byte[]> Segments { get; }

    /// <summary>
    /// Gets the length of the body.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Copies the body to a <see cref="PipeWriter"/>.
    /// </summary>
    /// <param name="destination">The destination</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);

        foreach (var segment in Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await destination.WriteAsync(segment, cancellationToken);
        }
    }
}
