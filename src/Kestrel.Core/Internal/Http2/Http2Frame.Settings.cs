// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Frame
    {
        private const int SettingSize = 6; // 2 bytes for the id, 4 bytes for the value.

        public Http2SettingsFrameFlags SettingsFlags
        {
            get => (Http2SettingsFrameFlags)Flags;
            set => Flags = (byte)value;
        }

        public int SettingsCount
        {
            get => Length / SettingSize;
            set => Length = value * SettingSize;
        }

        public IList<Http2PeerSetting> GetSettings()
        {
            var settings = new Http2PeerSetting[SettingsCount];
            for (int i = 0; i < settings.Length; i++)
            {
                settings[i] = GetSetting(i);
            }
            return settings;
        }

        private Http2PeerSetting GetSetting(int index)
        {
            var offset = index * SettingSize;
            var payload = Payload.Slice(offset);
            var id = (Http2SettingsParameter)BinaryPrimitives.ReadUInt16BigEndian(payload);
            var value = BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(2));

            return new Http2PeerSetting(id, value);
        }

        public void PrepareSettings(Http2SettingsFrameFlags flags, IList<Http2PeerSetting> settings = null)
        {
            var settingCount = settings?.Count ?? 0;
            SettingsCount = settingCount;
            Type = Http2FrameType.SETTINGS;
            SettingsFlags = flags;
            StreamId = 0;
            for (int i = 0; i < settingCount; i++)
            {
                SetSetting(i, settings[i]);
            }
        }

        private void SetSetting(int index, Http2PeerSetting setting)
        {
            var offset = index * SettingSize;
            var payload = Payload.Slice(offset);
            BinaryPrimitives.WriteUInt16BigEndian(payload, (ushort)setting.Parameter);
            BinaryPrimitives.WriteUInt32BigEndian(payload.Slice(2), setting.Value);
        }
    }
}
