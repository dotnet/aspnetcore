// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal interface ILibuvTrace : ILogger
    {
        void ConnectionRead(string connectionId, int count);

        void ConnectionReadFin(string connectionId);

        void ConnectionWriteFin(string connectionId, string reason);

        void ConnectionWrite(string connectionId, int count);

        void ConnectionWriteCallback(string connectionId, int status);

        void ConnectionError(string connectionId, Exception ex);

        void ConnectionReset(string connectionId);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);
    }
}
