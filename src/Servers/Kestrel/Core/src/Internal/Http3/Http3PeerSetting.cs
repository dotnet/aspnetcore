// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
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
}
