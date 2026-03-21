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

    /// <summary>
    /// Gets or sets a value indicating whether the enum type name prefix should be removed when reading and writing enum values.
    /// The default value is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Protocol Buffers, enum value names are globally scoped, so they are often prefixed with the enum type name
    /// to avoid name collisions. For example, the <c>Status</c> enum might define values like <c>STATUS_UNKNOWN</c>
    /// and <c>STATUS_OK</c>.
    /// </para>
    /// <code>
    /// enum Status {
    ///   STATUS_UNKNOWN = 0;
    ///   STATUS_OK = 1;
    /// }
    /// </code>
    /// <para>
    /// When <see cref="RemoveEnumPrefix"/> is set to <see langword="true"/>:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>The <c>STATUS</c> prefix is removed from enum values. The enum values above will be read and written as <c>UNKNOWN</c> and <c>OK</c> instead of <c>STATUS_UNKNOWN</c> and <c>STATUS_OK</c>.</description>
    /// </item>
    /// <item>
    /// <description>Original prefixed values are used as a fallback when reading JSON. For example, <c>STATUS_OK</c> and <c>OK</c> map to the <c>STATUS_OK</c> enum value.</description>
    /// </item>
    /// </list>
    /// <para>
    /// The Protobuf JSON specification requires enum values in JSON to match enum fields exactly.
    /// Enabling this option may reduce interoperability, as removing enum prefix might not be supported
    /// by other JSON transcoding implementations.
    /// </para>
    /// <para>
    /// For more information, see <see href="https://protobuf.dev/programming-guides/json/"/>.
    /// </para>
    /// </remarks>
    public bool RemoveEnumPrefix { get; set; }
}
