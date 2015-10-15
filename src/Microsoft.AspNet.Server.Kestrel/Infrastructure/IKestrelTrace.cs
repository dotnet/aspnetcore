using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Infrastructure
{
    public interface IKestrelTrace : ILogger
    {
        void ConnectionStart(long connectionId);

        void ConnectionStop(long connectionId);

        void ConnectionRead(long connectionId, int count);

        void ConnectionPause(long connectionId);

        void ConnectionResume(long connectionId);

        void ConnectionReadFin(long connectionId);

        void ConnectionWriteFin(long connectionId);

        void ConnectionWroteFin(long connectionId, int status);

        void ConnectionKeepAlive(long connectionId);

        void ConnectionDisconnect(long connectionId);

        void ConnectionWrite(long connectionId, int count);

        void ConnectionWriteCallback(long connectionId, int status);

        void ApplicationError(Exception ex);
    }
}