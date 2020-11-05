// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Internal
{
    internal interface IQuicTrace : ILogger
    {
        void NewConnection(string connectionId);
        void NewStream(string streamId);
        void ConnectionError(string connectionId, Exception ex);
        void StreamError(string streamId, Exception ex);
        void StreamPause(string streamId);
        void StreamResume(string streamId);
        void StreamShutdownWrite(string streamId, Exception ex);
        void StreamAbort(string streamId, Exception ex);
    }
}
