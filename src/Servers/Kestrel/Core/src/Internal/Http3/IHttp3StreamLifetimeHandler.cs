// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal interface IHttp3StreamLifetimeHandler
{
    void OnUnidentifiedStreamReceived(Http3PendingStream stream);
    void OnStreamCreated(IHttp3Stream stream);
    void OnStreamHeaderReceived(IHttp3Stream stream);
    void OnStreamCompleted(IHttp3Stream stream);
    void OnStreamConnectionError(Http3ConnectionErrorException ex);

    bool OnInboundControlStream(Http3ControlStream stream);
    bool OnInboundEncoderStream(Http3ControlStream stream);
    bool OnInboundDecoderStream(Http3ControlStream stream);
    void OnInboundControlStreamSetting(Http3SettingType type, long value);
}
