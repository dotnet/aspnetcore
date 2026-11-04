// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A media type with its associated quality.
/// </summary>
public readonly struct MediaTypeSegmentWithQuality
{
    /// <summary>
    /// Initializes an instance of <see cref="MediaTypeSegmentWithQuality"/>.
    /// </summary>
    /// <param name="mediaType">The <see cref="StringSegment"/> containing the media type.</param>
    /// <param name="quality">The quality parameter of the media type or 1 in the case it does not exist.</param>
    public MediaTypeSegmentWithQuality(StringSegment mediaType, double quality)
    {
        MediaType = mediaType;
        Quality = quality;
    }

    /// <summary>
    /// Gets the media type of this <see cref="MediaTypeSegmentWithQuality"/>.
    /// </summary>
    public StringSegment MediaType { get; }

    /// <summary>
    /// Gets the quality of this <see cref="MediaTypeSegmentWithQuality"/>.
    /// </summary>
    public double Quality { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        // For logging purposes
        return MediaType.ToString();
    }
}
