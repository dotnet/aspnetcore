// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal readonly struct Http3PeerSetting
{
    public Http3PeerSetting(Http3SettingType parameter, uint value)
    {
        Parameter = parameter;
        Value = value;
    }

    public Http3SettingType Parameter { get; }

    public uint Value { get; }
}
