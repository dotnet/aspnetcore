// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// Metadata used to negotiate wich endpoint to select based on the value of the Accept-Encoding header.
/// </summary>
/// <param name="value">The <c>Accept-Encoding</c> value this metadata represents.</param>
/// <param name="quality">The server preference to apply to break ties.</param>
public class ContentEncodingMetadata(string value, double quality) : INegotiateMetadata
{
    /// <summary>
    /// Gets the <c>Accept-Encoding</c> value this metadata represents.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Gets the server preference to apply to break ties when two or more client options have the same preference.
    /// </summary>
    public double Quality { get; } = quality;
}
