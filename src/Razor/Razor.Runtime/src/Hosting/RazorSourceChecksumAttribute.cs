// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Specifies the checksum of a source file that contributed to a compiled item.
/// </summary>
/// <remarks>
/// <para>
/// These attributes are added by the Razor infrastructure when generating code to assist runtime
/// implementations to determine the integrity of compiled items.
/// </para>
/// <para>
/// Runtime implementations should access the checksum metadata for an item using
/// <see cref="RazorCompiledItemExtensions.GetChecksumMetadata(RazorCompiledItem)"/>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RazorSourceChecksumAttribute : Attribute, IRazorSourceChecksumMetadata
{
    /// <summary>
    /// Creates a new <see cref="RazorSourceChecksumAttribute"/>.
    /// </summary>
    /// <param name="checksumAlgorithm">The algorithm used to create this checksum.</param>
    /// <param name="checksum">The checksum as a string of hex-encoded bytes.</param>
    /// <param name="identifier">The identifier associated with this thumbprint.</param>
    public RazorSourceChecksumAttribute(string checksumAlgorithm, string checksum, string identifier)
    {
        ArgumentNullException.ThrowIfNull(checksumAlgorithm);
        ArgumentNullException.ThrowIfNull(checksum);
        ArgumentNullException.ThrowIfNull(identifier);

        ChecksumAlgorithm = checksumAlgorithm;
        Checksum = checksum;
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the checksum as string of hex-encoded bytes.
    /// </summary>
    public string Checksum { get; }

    /// <summary>
    /// Gets the name of the algorithm used to create this checksum.
    /// </summary>
    public string ChecksumAlgorithm { get; }

    /// <summary>
    /// Gets the identifier of the source file associated with this checksum.
    /// </summary>
    public string Identifier { get; }
}
