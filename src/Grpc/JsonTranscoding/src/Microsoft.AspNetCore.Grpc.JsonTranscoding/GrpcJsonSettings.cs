// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding;

/// <summary>
/// Provides settings for serializing JSON.
/// </summary>
public sealed class GrpcJsonSettings
{
    /// <summary>
    /// Whether fields which would otherwise not be included in the formatted data
    /// should be formatted even when the value is not present, or has the default value.
    /// This option only affects fields which don't support "presence" (e.g.
    /// singular non-optional proto3 primitive fields).
    /// </summary>
    public bool IgnoreDefaultValues { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="Enum"/> values are written as integers instead of strings.
    /// Default value is false.
    /// </summary>
    public bool WriteEnumsAsIntegers { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether <see cref="Int64"/> and <see cref="UInt64"/> values are written as strings instead of numbers.
    /// Default value is false.
    /// </summary>
    public bool WriteInt64sAsStrings { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether JSON should use pretty printing.
    /// Default value is false.
    /// </summary>
    public bool WriteIndented { get; set; }
}
