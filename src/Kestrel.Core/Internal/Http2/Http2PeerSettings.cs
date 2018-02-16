// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2PeerSettings : IEnumerable<Http2PeerSetting>
    {
        public const uint DefaultHeaderTableSize = 4096;
        public const bool DefaultEnablePush = true;
        public const uint DefaultMaxConcurrentStreams = uint.MaxValue;
        public const uint DefaultInitialWindowSize = 65535;
        public const uint DefaultMaxFrameSize = 16384;
        public const uint DefaultMaxHeaderListSize = uint.MaxValue;

        public uint HeaderTableSize { get; set; } = DefaultHeaderTableSize;

        public bool EnablePush { get; set; } = DefaultEnablePush;

        public uint MaxConcurrentStreams { get; set; } = DefaultMaxConcurrentStreams;

        public uint InitialWindowSize { get; set; } = DefaultInitialWindowSize;

        public uint MaxFrameSize { get; set; } = DefaultMaxFrameSize;

        public uint MaxHeaderListSize { get; set; } = DefaultMaxHeaderListSize;

        public void ParseFrame(Http2Frame frame)
        {
            var settingsCount = frame.Length / 6;

            for (var i = 0; i < settingsCount; i++)
            {
                var offset = i * 6;
                var id = (Http2SettingsParameter)((frame.Payload[offset] << 8) | frame.Payload[offset + 1]);
                var value = (uint)((frame.Payload[offset + 2] << 24)
                    | (frame.Payload[offset + 3] << 16)
                    | (frame.Payload[offset + 4] << 8)
                    | frame.Payload[offset + 5]);

                switch (id)
                {
                    case Http2SettingsParameter.SETTINGS_HEADER_TABLE_SIZE:
                        HeaderTableSize = value;
                        break;
                    case Http2SettingsParameter.SETTINGS_ENABLE_PUSH:
                        if (value != 0 && value != 1)
                        {
                            throw new Http2SettingsParameterOutOfRangeException(Http2SettingsParameter.SETTINGS_ENABLE_PUSH,
                                lowerBound: 0,
                                upperBound: 1);
                        }

                        EnablePush = value == 1;
                        break;
                    case Http2SettingsParameter.SETTINGS_MAX_CONCURRENT_STREAMS:
                        MaxConcurrentStreams = value;
                        break;
                    case Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE:
                        if (value > int.MaxValue)
                        {
                            throw new Http2SettingsParameterOutOfRangeException(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE,
                                lowerBound: 0,
                                upperBound: int.MaxValue);
                        }

                        InitialWindowSize = value;
                        break;
                    case Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE:
                        if (value <  Http2Frame.MinAllowedMaxFrameSize || value > Http2Frame.MaxAllowedMaxFrameSize)
                        {
                            throw new Http2SettingsParameterOutOfRangeException(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE,
                                lowerBound: Http2Frame.MinAllowedMaxFrameSize,
                                upperBound: Http2Frame.MaxAllowedMaxFrameSize);
                        }

                        MaxFrameSize = value;
                        break;
                    case Http2SettingsParameter.SETTINGS_MAX_HEADER_LIST_SIZE:
                        MaxHeaderListSize = value;
                        break;
                    default:
                        // http://httpwg.org/specs/rfc7540.html#rfc.section.6.5.2
                        //
                        // An endpoint that receives a SETTINGS frame with any unknown or unsupported identifier MUST ignore that setting.
                        break;
                }
            }
        }

        public IEnumerator<Http2PeerSetting> GetEnumerator()
        {
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_HEADER_TABLE_SIZE, HeaderTableSize);
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_ENABLE_PUSH, EnablePush ? 1u : 0);
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_CONCURRENT_STREAMS, MaxConcurrentStreams);
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, InitialWindowSize);
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, MaxFrameSize);
            yield return new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_HEADER_LIST_SIZE, MaxHeaderListSize);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
