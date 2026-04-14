// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Metadata used to negotiate wich endpoint to select based on the value of the Accept-Encoding header.
/// </summary>
public sealed class ContentEncodingMetadata : INegotiateMetadata
{
    /// <summary>
    /// Initializes a new instance of <see cref="ContentEncodingMetadata"/>.
    /// </summary>
    /// <param name="value">The <c>Accept-Encoding</c> value this metadata represents.</param>
    /// <param name="quality">The server preference to apply to break ties.</param>
    public ContentEncodingMetadata(string value, double quality)
    {
        Value = value;
        Quality = quality;
    }

    /// <summary>
    /// Gets the <c>Accept-Encoding</c> value this metadata represents.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the server preference to apply to break ties when two or more client options have the same preference.
    /// </summary>
    public double Quality { get; }

    /// <summary>
    /// Gets the expected SHA-256 hash of the compression dictionary required for this encoding,
    /// or <see langword="null"/> if no dictionary is required.
    /// </summary>
    /// <remarks>
    /// When set, the endpoint is only considered a valid match if the client sends an
    /// <c>Available-Dictionary</c> request header whose hash matches this value.
    /// This is used with dictionary-compressed encodings such as <c>dcb</c> and <c>dcz</c>
    /// as defined by RFC 9842 (Compression Dictionary Transport).
    /// </remarks>
    public string? DictionaryHash { get; init; }
}
