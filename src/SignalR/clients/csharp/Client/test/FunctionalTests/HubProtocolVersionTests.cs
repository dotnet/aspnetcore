// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public class HubProtocolVersionTestsCollection : ICollectionFixture<InProcessTestServer<VersionStartup>>
{
    public const string Name = nameof(HubProtocolVersionTestsCollection);
}

[Collection(HubProtocolVersionTestsCollection.Name)]
public class HubProtocolVersionTests : FunctionalTestBase
{
    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task ClientUsingOldCallWithOriginalProtocol(HttpTransportType transportType)
    {
        await using (var server = await StartServer<VersionStartup>())
        {
            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/version", transportType);

            var connection = connectionBuilder.Build();

            try
            {
                await connection.StartAsync().DefaultTimeout();

                var result = await connection.InvokeAsync<string>(nameof(VersionHub.Echo), "Hello World!").DefaultTimeout();

                Assert.Equal("Hello World!", result);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task ClientUsingOldCallWithNewProtocol(HttpTransportType transportType)
    {
        await using (var server = await StartServer<VersionStartup>())
        {
            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/version", transportType);
            connectionBuilder.Services.AddSingleton<IHubProtocol>(new VersionedJsonHubProtocol(1000));

            var connection = connectionBuilder.Build();

            try
            {
                await connection.StartAsync().DefaultTimeout();

                var result = await connection.InvokeAsync<string>(nameof(VersionHub.Echo), "Hello World!").DefaultTimeout();

                Assert.Equal("Hello World!", result);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    public async Task ClientUsingNewCallWithNewProtocol(HttpTransportType transportType)
    {
        await using (var server = await StartServer<VersionStartup>())
        {
            var httpConnectionFactory = new HttpConnectionFactory(
                Options.Create(new HttpConnectionOptions
                {
                    Transports = transportType,
                    DefaultTransferFormat = TransferFormat.Text
                }),
                LoggerFactory);
            var tcs = new TaskCompletionSource();

            var proxyConnectionFactory = new ProxyConnectionFactory(httpConnectionFactory);

            var connectionBuilder = new HubConnectionBuilder()
                .WithUrl(new Uri(server.Url + "/version"))
                .WithLoggerFactory(LoggerFactory);
            connectionBuilder.Services.AddSingleton<IHubProtocol>(new VersionedJsonHubProtocol(1000));
            connectionBuilder.Services.AddSingleton<IConnectionFactory>(proxyConnectionFactory);

            var connection = connectionBuilder.Build();
            connection.On("NewProtocolMethodClient", () =>
            {
                tcs.SetResult();
            });

            try
            {
                await connection.StartAsync().DefaultTimeout();

                // Task should already have been awaited in StartAsync
                var connectionContext = await proxyConnectionFactory.ConnectTask.DefaultTimeout();

                // Simulate a new call from the client
                var messageToken = new JObject
                {
                    ["type"] = int.MaxValue
                };

                connectionContext.Transport.Output.Write(Encoding.UTF8.GetBytes(messageToken.ToString()));
                connectionContext.Transport.Output.Write(new[] { (byte)0x1e });
                await connectionContext.Transport.Output.FlushAsync().DefaultTimeout();

                await tcs.Task.DefaultTimeout();
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    [Theory]
    [MemberData(nameof(TransportTypes))]
    [LogLevel(LogLevel.Trace)]
    public async Task ClientWithUnsupportedProtocolVersionDoesNotConnect(HttpTransportType transportType)
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName;
        }

        await using (var server = await StartServer<VersionStartup>(ExpectedErrors))
        {
            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + "/version", transportType);
            connectionBuilder.Services.AddSingleton<IHubProtocol>(new SingleVersionHubProtocol(new VersionedJsonHubProtocol(int.MaxValue), int.MaxValue));

            var connection = connectionBuilder.Build();

            try
            {
                await ExceptionAssert.ThrowsAsync<HubException>(
                    () => connection.StartAsync(),
                    "Unable to complete handshake with the server due to an error: The server does not support version 2147483647 of the 'json' protocol.").DefaultTimeout();
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }

    public class SingleVersionHubProtocol : IHubProtocol
    {
        private readonly IHubProtocol _protocol;
        private readonly int _version;

        public SingleVersionHubProtocol(IHubProtocol inner, int version)
        {
            _protocol = inner;
            _version = version;
        }

        public string Name => _protocol.Name;

        public int Version => _version;

        public TransferFormat TransferFormat => _protocol.TransferFormat;

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message) => _protocol.GetMessageBytes(message);

        public bool IsVersionSupported(int version)
        {
            return version == _version;
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage message)
        {
            return _protocol.TryParseMessage(ref input, binder, out message);
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            _protocol.WriteMessage(message, output);
        }
    }

    private class ProxyConnectionFactory : IConnectionFactory
    {
        private readonly IConnectionFactory _innerFactory;
        public ValueTask<ConnectionContext> ConnectTask { get; private set; }

        public ProxyConnectionFactory(IConnectionFactory innerFactory)
        {
            _innerFactory = innerFactory;
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            ConnectTask = _innerFactory.ConnectAsync(endPoint, cancellationToken);
            return ConnectTask;
        }
    }

    public static IEnumerable<object[]> TransportTypes()
    {
        if (TestHelpers.IsWebSocketsSupported())
        {
            yield return new object[] { HttpTransportType.WebSockets };
        }
        yield return new object[] { HttpTransportType.ServerSentEvents };
        yield return new object[] { HttpTransportType.LongPolling };
    }
}
