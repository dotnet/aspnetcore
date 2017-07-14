// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    [CollectionDefinition(Name)]
    public class HubConnectionTestsCollection : ICollectionFixture<ServerFixture<Startup>>
    {
        public const string Name = "EndToEndTests";
    }

    [Collection(HubConnectionTestsCollection.Name)]
    public class HubConnectionTests : LoggedTest
    {
        private readonly ServerFixture<Startup> _serverFixture;
        public HubConnectionTests(ServerFixture<Startup> serverFixture, ITestOutputHelper output)
            : base(output)
        {
            if (serverFixture == null)
            {
                throw new ArgumentNullException(nameof(serverFixture));
            }

            _serverFixture = serverFixture;
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task CheckFixedMessage(IHubProtocol protocol, TransportType transportType, string path)
        {
            using (StartLog(out var loggerFactory))
            {

                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + path), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>("HelloWorld").OrTimeout();

                    Assert.Equal("Hello World!", result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task CanSendAndReceiveMessage(IHubProtocol protocol, TransportType transportType, string path)
        {
            using (StartLog(out var loggerFactory))
            {
                const string originalMessage = "SignalR";
                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + path), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>("Echo", originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task MethodsAreCaseInsensitive(IHubProtocol protocol, TransportType transportType, string path)
        {
            using (StartLog(out var loggerFactory))
            {
                const string originalMessage = "SignalR";
                var uriString = "http://test/" + path;
                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + path), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>("echo", originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task CanInvokeClientMethodFromServer(IHubProtocol protocol, TransportType transportType, string path)
        {
            using (StartLog(out var loggerFactory))
            {
                const string originalMessage = "SignalR";

                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + path), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var tcs = new TaskCompletionSource<string>();
                    connection.On<string>("Echo", tcs.SetResult);

                    await connection.InvokeAsync("CallEcho", originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, await tcs.Task.OrTimeout());
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task CanStreamClientMethodFromServer(IHubProtocol protocol, TransportType transportType, string path)
        {
            using (StartLog(out var loggerFactory))
            {
                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + path), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var tcs = new TaskCompletionSource<string>();

                    var results = await connection.Stream<string>("Stream").ReadAllAsync().OrTimeout();

                    Assert.Equal(new[] { "a", "b", "c" }, results.ToArray());
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsXTransportsXHubPaths))]
        public async Task ServerClosesConnectionIfHubMethodCannotBeResolved(IHubProtocol hubProtocol, TransportType transportType, string hubPath)
        {
            using (StartLog(out var loggerFactory))
            {
                var httpConnection = new HttpConnection(new Uri(_serverFixture.BaseUrl + hubPath), transportType, loggerFactory);
                var connection = new HubConnection(httpConnection, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAnyAsync<Exception>(
                        async () => await connection.InvokeAsync("!@#$%")).OrTimeout();

                    Assert.Equal("Unknown hub method '!@#$%'", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "Exception from test");
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        public static IEnumerable<object[]> HubProtocolsXTransportsXHubPaths()
        {
            foreach (var protocol in HubProtocols)
            {
                foreach (var transport in TransportTypes())
                {
                    foreach (var hubPath in HubPaths)
                    {
                        yield return new object[] { protocol, transport, hubPath };
                    }
                }
            }
        }

        public static string[] HubPaths = new[] { "/default", "/dynamic" };

        public static IEnumerable<IHubProtocol> HubProtocols =>
            new IHubProtocol[]
            {
                new JsonHubProtocol(new JsonSerializer()),
                new MessagePackHubProtocol(),
            };


        public static IEnumerable<TransportType> TransportTypes()
        {
            if (WebsocketsSupported())
            {
                // TODO: Currently we are always sending Text messages over websockets which does not work
                // with binary protocols. It is getting fixed separately.
                // The tests are also failing on full framework when using WebSockets transport
                // due to: https://github.com/aspnet/SignalR/issues/568
                // yield return TransportType.WebSockets;
            }
            yield return TransportType.ServerSentEvents;
            yield return TransportType.LongPolling;

            bool WebsocketsSupported()
            {
                try
                {
                    new System.Net.WebSockets.ClientWebSocket();
                }
                catch (PlatformNotSupportedException)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
