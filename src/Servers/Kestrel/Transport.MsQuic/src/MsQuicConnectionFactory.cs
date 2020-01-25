// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic
{
    public class MsQuicConnectionFactory : IConnectionFactory
    {
        private MsQuicApi _api;
        private QuicSession _session;
        private bool _started;
        private MsQuicTransportContext _transportContext;

        public MsQuicConnectionFactory(IOptions<MsQuicTransportOptions> options, IHostApplicationLifetime lifetime, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _api = new MsQuicApi();
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Client");
            var trace = new MsQuicTrace(logger);

            _transportContext = new MsQuicTransportContext(lifetime, trace, options.Value);
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            if (!(endPoint is IPEndPoint ipEndPoint))
            {
                throw new NotSupportedException($"{endPoint} is not supported");
            }

            if (!_started)
            {
                _started = true;
                await StartAsync();
            }

            var connection = await _session.ConnectionOpenAsync(ipEndPoint, _transportContext);
            return connection;
        }

        private ValueTask StartAsync()
        {
            _api.RegistrationOpen(Encoding.ASCII.GetBytes(_transportContext.Options.RegistrationName));
            _session = _api.SessionOpen(_transportContext.Options.Alpn);
            _session.SetIdleTimeout(_transportContext.Options.IdleTimeout);
            _session.SetPeerBiDirectionalStreamCount(_transportContext.Options.MaxBidirectionalStreamCount);
            _session.SetPeerUnidirectionalStreamCount(_transportContext.Options.MaxBidirectionalStreamCount);
            return new ValueTask();
        }
    }
}
