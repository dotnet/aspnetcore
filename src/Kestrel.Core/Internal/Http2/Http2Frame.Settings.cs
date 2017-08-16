// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        public Http2SettingsFrameFlags SettingsFlags
        {
            get => (Http2SettingsFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public void PrepareSettings(Http2SettingsFrameFlags flags, Http2PeerSettings settings = null)
        {
            var settingCount = settings?.Count() ?? 0;

            Length = 6 * settingCount;
            Type = Http2FrameType.SETTINGS;
            SettingsFlags = flags;
            StreamId = 0;

            if (settings != null)
            {
                Span<byte> payload = Payload;
                foreach (var setting in settings)
                {
                    payload[0] = (byte)((ushort)setting.Parameter >> 8);
                    payload[1] = (byte)(ushort)setting.Parameter;
                    payload[2] = (byte)(setting.Value >> 24);
                    payload[3] = (byte)(setting.Value >> 16);
                    payload[4] = (byte)(setting.Value >> 8);
                    payload[5] = (byte)setting.Value;
                    payload = payload.Slice(6);
                }
            }
        }
    }
}
