using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public interface IKestrelTrace : ILogger
    {
        void ConnectionStart(string connectionId);

        void ConnectionStop(string connectionId);

        void ConnectionRead(string connectionId, int count);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);

        void ConnectionReadFin(string connectionId);

        void ConnectionWriteFin(string connectionId);

        void ConnectionWroteFin(string connectionId, int status);

        void ConnectionKeepAlive(string connectionId);

        void ConnectionDisconnect(string connectionId);

        void ConnectionWrite(string connectionId, int count);

        void ConnectionWriteCallback(string connectionId, int status);

        void ConnectionError(string connectionId, Exception ex);

        void ConnectionDisconnectedWrite(string connectionId, int count, Exception ex);

        void NotAllConnectionsClosedGracefully();

        void ApplicationError(Exception ex);
    }
}