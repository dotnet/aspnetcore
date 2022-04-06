// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents a cached response body.
/// </summary>
public class CachedResponseBody
{
    /// <summary>
    /// Creates a new <see cref="CachedResponseBody"/> instance.
    /// </summary>
    /// <param name="segments">The segments.</param>
    /// <param name="length">The length.</param>
    public CachedResponseBody(List<byte[]> segments, long length)
    {
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
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));

        foreach (var segment in Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Copy(segment, destination);

            await destination.FlushAsync(cancellationToken);
        }
    }

    private static void Copy(byte[] segment, PipeWriter destination)
    {
        var span = destination.GetSpan(segment.Length);

        segment.CopyTo(span);
        destination.Advance(segment.Length);
    }
}
