// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    public class MsQuicConnectionFactory : IConnectionFactory
    {
        // For client side, TODO clean this all up.
        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {

            return new ValueTask<ConnectionContext>();
        }

        internal QUIC_STATUS HandleEvent(
           QuicConnection connection,
           ref NativeMethods.ConnectionEvent connectionEvent)
        {
            var status = QUIC_STATUS.SUCCESS;
            switch (connectionEvent.Type)
            {
                case QUIC_CONNECTION_EVENT.CONNECTED:
                    {
                        // NOOP
                    }
                    break;
                    //case QUIC_CONNECTION_EVENT.NEW_STREAM:
                    //    {
                    //        status = HandleEventNewStream(
                    //            connection,
                    //            (QuicSession.ConnectionEventNewStream)connectionEvent);
                    //    }
                    //    break;
                    //case QUIC_CONNECTION_EVENT.IDEAL_SEND_BUFFER:
                    //    {
                    //        status = HandleEventIdealSendBuffer(
                    //            connection,
                    //            (QuicSession.ConnectionEventIdealSendBuffer)connectionEvent);
                    //    }
                    //    break;

            }
            return status;
        }
    }
}
