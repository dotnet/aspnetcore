// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// A metadata object containing the checksum of a source file that contributed to a compiled item.
/// </summary>
public interface IRazorSourceChecksumMetadata
{
    /// <summary>
    /// Gets the checksum as string of hex-encoded bytes.
    /// </summary>
    string Checksum { get; }

    /// <summary>
    /// Gets the name of the algorithm used to create this checksum.
    /// </summary>
    string ChecksumAlgorithm { get; }

    /// <summary>
    /// Gets the identifier of the source file associated with this checksum.
    /// </summary>
    string Identifier { get; }
}
