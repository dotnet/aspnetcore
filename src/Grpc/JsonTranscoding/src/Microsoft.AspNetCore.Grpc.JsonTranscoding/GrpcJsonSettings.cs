// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding;

// TODO - improve names. boolean property values should aim to be false
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public sealed class GrpcJsonSettings
{
    /// <summary>
    /// Whether fields which would otherwise not be included in the formatted data
    /// should be formatted even when the value is not present, or has the default value.
    /// This option only affects fields which don't support "presence" (e.g.
    /// singular non-optional proto3 primitive fields).
    /// </summary>
    public bool IgnoreDefaultValues { get; set; }

    public bool WriteEnumsAsIntegers { get; set; }

    public bool WriteInt64sAsStrings { get; set; }

    public bool WriteIndented { get; set; }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
