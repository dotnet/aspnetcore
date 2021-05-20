// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal interface IHttp3StreamLifetimeHandler
    {
        void OnStreamCreated(IHttp3Stream stream);
        void OnStreamHeaderReceived(IHttp3Stream stream);
        void OnStreamCompleted(IHttp3Stream stream);
        void OnStreamConnectionError(Http3ConnectionErrorException ex);

        bool OnInboundControlStream(Http3ControlStream stream);
        bool OnInboundEncoderStream(Http3ControlStream stream);
        bool OnInboundDecoderStream(Http3ControlStream stream);
        void OnInboundControlStreamSetting(Http3SettingType type, long value);
    }
}
