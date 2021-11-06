// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal readonly struct Http2PeerSetting
{
    public Http2PeerSetting(Http2SettingsParameter parameter, uint value)
    {
        Parameter = parameter;
        Value = value;
    }

    public Http2SettingsParameter Parameter { get; }

    public uint Value { get; }
}
