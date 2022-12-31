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
    /// Default value is false.
    /// </summary>
    public bool IgnoreDefaultValues { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="Enum"/> values are written as integers instead of strings.
    /// Default value is false.
    /// </summary>
    public bool WriteEnumsAsIntegers { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="long"/> and <see cref="ulong"/> values are written as strings instead of numbers.
    /// Default value is false.
    /// </summary>
    public bool WriteInt64sAsStrings { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether JSON should use pretty printing.
    /// Default value is false.
    /// </summary>
    public bool WriteIndented { get; set; }
}
