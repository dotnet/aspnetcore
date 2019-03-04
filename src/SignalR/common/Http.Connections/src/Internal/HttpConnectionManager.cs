// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public partial class HttpConnectionManager
    {
        // TODO: Consider making this configurable? At least for testing?
        private static readonly TimeSpan _heartbeatTickRate = TimeSpan.FromSeconds(1);

        private static readonly RNGCryptoServiceProvider _keyGenerator = new RNGCryptoServiceProvider();

        private readonly ConcurrentDictionary<string, (HttpConnectionContext Connection, ValueStopwatch Timer)> _connections =
            new ConcurrentDictionary<string, (HttpConnectionContext Connection, ValueStopwatch Timer)>(StringComparer.Ordinal);
        private readonly TimerAwaitable _nextHeartbeat;
        private readonly ILogger<HttpConnectionManager> _logger;
        private readonly ILogger<HttpConnectionContext> _connectionLogger;
        private readonly TimeSpan _disconnectTimeout;

        public HttpConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime)
            : this(loggerFactory, appLifetime, Options.Create(new ConnectionOptions() { DisconnectTimeout = ConnectionOptionsSetup.DefaultDisconectTimeout }))
        {
        }

        public HttpConnectionManager(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IOptions<ConnectionOptions> connectionOptions)
        {
            _logger = loggerFactory.CreateLogger<HttpConnectionManager>();
            _connectionLogger = loggerFactory.CreateLogger<HttpConnectionContext>();
            _nextHeartbeat = new TimerAwaitable(_heartbeatTickRate, _heartbeatTickRate);
            _disconnectTimeout = connectionOptions.Value.DisconnectTimeout ?? ConnectionOptionsSetup.DefaultDisconectTimeout;
            // Register these last as the callbacks could run immediately
            appLifetime.ApplicationStarted.Register(() => Start());
            appLifetime.ApplicationStopping.Register(() => CloseConnections());
        }

        public void Start()
        {
            _nextHeartbeat.Start();

            // Start the timer loop
            _ = ExecuteTimerLoop();
        }

        public bool TryGetConnection(string id, out HttpConnectionContext connection)
        {
            connection = null;

            if (_connections.TryGetValue(id, out var pair))
            {
                connection = pair.Connection;
                return true;
            }
            return false;
        }

        public HttpConnectionContext CreateConnection()
        {
            return CreateConnection(PipeOptions.Default, PipeOptions.Default);
        }

        /// <summary>
        /// Creates a connection without Pipes setup to allow saving allocations until Pipes are needed.
        /// </summary>
        /// <returns></returns>
        public HttpConnectionContext CreateConnection(PipeOptions transportPipeOptions, PipeOptions appPipeOptions)
        {
            var id = MakeNewConnectionId();

            Log.CreatedNewConnection(_logger, id);
            var connectionTimer = HttpConnectionsEventSource.Log.ConnectionStart(id);
            var connection = new HttpConnectionContext(id, _connectionLogger);
            var pair = DuplexPipe.CreateConnectionPair(transportPipeOptions, appPipeOptions);
            connection.Transport = pair.Application;
            connection.Application = pair.Transport;

            _connections.TryAdd(id, (connection, connectionTimer));
            return connection;
        }

        public void RemoveConnection(string id)
        {
            if (_connections.TryRemove(id, out var pair))
            {
                // Remove the connection completely
                HttpConnectionsEventSource.Log.ConnectionStop(id, pair.Timer);
                Log.RemovedConnection(_logger, id);
            }
        }

        private static string MakeNewConnectionId()
        {
            // TODO: Use Span when WebEncoders implements Span methods https://github.com/aspnet/Home/issues/2966
            // 128 bit buffer / 8 bits per byte = 16 bytes
            var buffer = new byte[16];
            _keyGenerator.GetBytes(buffer);
            // Generate the id with RNGCrypto because we want a cryptographically random id, which GUID is not
            return WebEncoders.Base64UrlEncode(buffer);
        }

        private async Task ExecuteTimerLoop()
        {
            Log.HeartBeatStarted(_logger);

            // Dispose the timer when all the code consuming callbacks has completed
            using (_nextHeartbeat)
            {
                // The TimerAwaitable will return true until Stop is called
                while (await _nextHeartbeat)
                {
                    try
                    {
                        await ScanAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.ScanningConnectionsFailed(_logger, ex);
                    }
                }
            }

            Log.HeartBeatEnded(_logger);
        }

        public Task ScanAsync()
        {
            // Scan the registered connections looking for ones that have timed out
            foreach (var c in _connections)
            {
                var connection = c.Value.Connection;
                // Capture the connection state
                var lastSeenUtc = connection.LastSeenUtcIfInactive;

                // Once the decision has been made to dispose we don't check the status again
                // But don't clean up connections while the debugger is attached.
                if (!Debugger.IsAttached && lastSeenUtc.HasValue && (DateTimeOffset.UtcNow - lastSeenUtc.Value).TotalSeconds > _disconnectTimeout.TotalSeconds)
                {
                    Log.ConnectionTimedOut(_logger, connection.ConnectionId);
                    HttpConnectionsEventSource.Log.ConnectionTimedOut(connection.ConnectionId);

                    // This is most likely a long polling connection. The transport here ends because
                    // a poll completed and has been inactive for > 5 seconds so we wait for the 
                    // application to finish gracefully
                    _ = DisposeAndRemoveAsync(connection, closeGracefully: true);
                }
                else
                {
                    // Tick the heartbeat, if the connection is still active
                    connection.TickHeartbeat();
                }
            }

            return Task.CompletedTask;
        }

        public void CloseConnections()
        {
            // Stop firing the timer
            _nextHeartbeat.Stop();

            var tasks = new List<Task>();

            // REVIEW: In the future we can consider a hybrid where we first try to wait for shutdown
            // for a certain time frame then after some grace period we shutdown more aggressively
            foreach (var c in _connections)
            {
                // We're shutting down so don't wait for closing the application
                tasks.Add(DisposeAndRemoveAsync(c.Value.Connection, closeGracefully: false));
            }

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
        }

        public async Task DisposeAndRemoveAsync(HttpConnectionContext connection, bool closeGracefully)
        {
            try
            {
                await connection.DisposeAsync(closeGracefully);
            }
            catch (IOException ex)
            {
                Log.ConnectionReset(_logger, connection.ConnectionId, ex);
            }
            catch (WebSocketException ex) when (ex.InnerException is IOException)
            {
                Log.ConnectionReset(_logger, connection.ConnectionId, ex);
            }
            catch (Exception ex)
            {
                Log.FailedDispose(_logger, connection.ConnectionId, ex);
            }
            finally
            {
                // Remove it from the list after disposal so that's it's easy to see
                // connections that might be in a hung state via the connections list
                RemoveConnection(connection.ConnectionId);
            }
        }
    }
}
