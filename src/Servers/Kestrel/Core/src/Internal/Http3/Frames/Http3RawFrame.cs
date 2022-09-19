// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace System.Net.Http;

#pragma warning disable CA1852 // Seal internal types
internal partial class Http3RawFrame
#pragma warning restore CA1852 // Seal internal types
{
    public long Length { get; set; }

    public Http3FrameType Type { get; internal set; }

    public string FormattedType => Http3Formatting.ToFormattedType(Type);

    public override string ToString()
    {
        return $"{FormattedType} Length: {Length}";
    }
}
