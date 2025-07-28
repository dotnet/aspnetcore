// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding;

/// <summary>
/// Provides settings for serializing JSON.
/// </summary>
public sealed class GrpcJsonSettings
{
    /// <summary>
    /// Gets or sets a value that indicates whether fields with default values are ignored during serialization.
    /// This setting only affects fields which don't support "presence", such as singular non-optional proto3 primitive fields.
    /// Default value is <see langword="false"/>.
    /// </summary>
    public bool IgnoreDefaultValues { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="Enum"/> values are written as integers instead of strings.
    /// Default value is <see langword="false"/>.
    /// </summary>
    public bool WriteEnumsAsIntegers { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="long"/> and <see cref="ulong"/> values are written as strings instead of numbers.
    /// Default value is <see langword="false"/>.
    /// </summary>
    public bool WriteInt64sAsStrings { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether JSON should use pretty printing.
    /// Default value is <see langword="false"/>.
    /// </summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether property names are compared using case-insensitive matching during deserialization.
    /// The default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Protobuf JSON specification requires JSON property names to match message field names exactly, including case.
    /// Enabling this option may reduce interoperability, as case-insensitive property matching might not be supported
    /// by other JSON transcoding implementations.
    /// </para>
    /// <para>
    /// For more information, see <see href="https://protobuf.dev/programming-guides/json/"/>.
    /// </para>
    /// </remarks>
    public bool PropertyNameCaseInsensitive { get; set; }
}
