// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

/// <summary>
/// Tracks HTTP/2 settings for a peer (client or server).
/// </summary>
/// <remarks>
/// There are expected to be exactly two instances, one for the client and one for the server.
/// Both are owned by the <see cref="Http2Connection"/>.
/// </remarks>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc9113#name-defined-settings"/>
internal sealed class Http2PeerSettings
{
    // Note these are protocol defaults, not Kestrel defaults.
    public const uint DefaultHeaderTableSize = 4096;
    public const bool DefaultEnablePush = true;
    public const uint DefaultMaxConcurrentStreams = uint.MaxValue;
    public const uint DefaultInitialWindowSize = 65535;
    public const uint DefaultMaxFrameSize = MinAllowedMaxFrameSize;
    public const uint DefaultMaxHeaderListSize = uint.MaxValue;
    public const uint MaxWindowSize = int.MaxValue;
    internal const int MinAllowedMaxFrameSize = 16 * 1024;
    internal const int MaxAllowedMaxFrameSize = 16 * 1024 * 1024 - 1;

    public uint HeaderTableSize { get; set; } = DefaultHeaderTableSize;

    public bool EnablePush { get; set; } = DefaultEnablePush;

    public uint MaxConcurrentStreams { get; set; } = DefaultMaxConcurrentStreams;

    public uint InitialWindowSize { get; set; } = DefaultInitialWindowSize;

    public uint MaxFrameSize { get; set; } = DefaultMaxFrameSize;

    public uint MaxHeaderListSize { get; set; } = DefaultMaxHeaderListSize;

    // TODO: Return the diff so we can react
    public void Update(IList<Http2PeerSetting> settings)
    {
        foreach (var setting in settings)
        {
            var value = setting.Value;

            switch (setting.Parameter)
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
                    if (value > MaxWindowSize)
                    {
                        throw new Http2SettingsParameterOutOfRangeException(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE,
                            lowerBound: 0,
                            upperBound: MaxWindowSize);
                    }

                    InitialWindowSize = value;
                    break;
                case Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE:
                    if (value < MinAllowedMaxFrameSize || value > MaxAllowedMaxFrameSize)
                    {
                        throw new Http2SettingsParameterOutOfRangeException(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE,
                            lowerBound: MinAllowedMaxFrameSize,
                            upperBound: MaxAllowedMaxFrameSize);
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

    // Gets the settings that are different from the protocol defaults (as opposed to the server defaults).
    internal List<Http2PeerSetting> GetNonProtocolDefaults()
    {
        // By default, there are only two settings that are sent from server to client.
        // Set capacity to that value.
        var list = new List<Http2PeerSetting>(2);

        if (HeaderTableSize != DefaultHeaderTableSize)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_HEADER_TABLE_SIZE, HeaderTableSize));
        }

        if (EnablePush != DefaultEnablePush)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_ENABLE_PUSH, EnablePush ? 1u : 0));
        }

        if (MaxConcurrentStreams != DefaultMaxConcurrentStreams)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_CONCURRENT_STREAMS, MaxConcurrentStreams));
        }

        if (InitialWindowSize != DefaultInitialWindowSize)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, InitialWindowSize));
        }

        if (MaxFrameSize != DefaultMaxFrameSize)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE, MaxFrameSize));
        }

        if (MaxHeaderListSize != DefaultMaxHeaderListSize)
        {
            list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_MAX_HEADER_LIST_SIZE, MaxHeaderListSize));
        }

        list.Add(new Http2PeerSetting(Http2SettingsParameter.SETTINGS_ENABLE_CONNECT_PROTOCOL, 1u));

        return list;
    }
}
