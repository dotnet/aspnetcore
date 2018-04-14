// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
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
    public class HubConnectionTests : VerifiableLoggedTest
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

        private HubConnection CreateHubConnection(
            string path = null,
            HttpTransportType? transportType = null,
            IHubProtocol protocol = null,
            ILoggerFactory loggerFactory = null)
        {
            var hubConnectionBuilder = new HubConnectionBuilder();
            hubConnectionBuilder.Services.AddSingleton(protocol);
            hubConnectionBuilder.WithLoggerFactory(loggerFactory);

            var delegateConnectionFactory = new DelegateConnectionFactory(
                GetHttpConnectionFactory(loggerFactory, path, transportType ?? HttpTransportType.LongPolling | HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents),
                connection => ((HttpConnection)connection).DisposeAsync());
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            return hubConnectionBuilder.Build();
        }

        private Func<TransferFormat, Task<ConnectionContext>> GetHttpConnectionFactory(ILoggerFactory loggerFactory, string path, HttpTransportType transportType)
        {
            return async format =>
            {
                var connection = new HttpConnection(new Uri(_serverFixture.Url + path), transportType, loggerFactory);
                await connection.StartAsync(format);
                return connection;
            };
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CheckFixedMessage(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(CheckFixedMessage)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connectionBuilder = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + path, transportType);
                connectionBuilder.Services.AddSingleton(protocol);

                var connection = connectionBuilder.Build();

                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>(nameof(TestHub.HelloWorld)).OrTimeout();

                    Assert.Equal("Hello World!", result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanSendAndReceiveMessage(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(CanSendAndReceiveMessage)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanStopAndStartConnection(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(CanStopAndStartConnection)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();
                    Assert.Equal(originalMessage, result);
                    await connection.StopAsync().OrTimeout();
                    await connection.StartAsync().OrTimeout();
                    result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();
                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanStartConnectionFromClosedEvent(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(CanStartConnectionFromClosedEvent)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var logger = loggerFactory.CreateLogger<HubConnectionTests>();
                const string originalMessage = "SignalR";

                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                var restartTcs = new TaskCompletionSource<object>();
                connection.Closed += async e =>
                {
                    try
                    {
                        logger.LogInformation("Closed event triggered");
                        if (!restartTcs.Task.IsCompleted)
                        {
                            logger.LogInformation("Restarting connection");
                            await connection.StartAsync().OrTimeout();
                            logger.LogInformation("Restarted connection");
                            restartTcs.SetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        // It's important to try catch here since this happens
                        // on a thread pool thread
                        restartTcs.TrySetException(ex);
                    }
                };

                try
                {
                    await connection.StartAsync().OrTimeout();
                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();
                    Assert.Equal(originalMessage, result);

                    logger.LogInformation("Stopping connection");
                    await connection.StopAsync().OrTimeout();

                    logger.LogInformation("Waiting for reconnect");
                    await restartTcs.Task.OrTimeout();
                    logger.LogInformation("Reconnection complete");

                    result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();
                    Assert.Equal(originalMessage, result);

                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task MethodsAreCaseInsensitive(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(MethodsAreCaseInsensitive)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo).ToLowerInvariant(), originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanInvokeClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(CanInvokeClientMethodFromServer)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                const string originalMessage = "SignalR";

                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
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
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task InvokeNonExistantClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(InvokeNonExistantClientMethodFromServer)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                var closeTcs = new TaskCompletionSource<object>();
                connection.Closed += e =>
                {
                    if (e != null)
                    {
                        closeTcs.SetException(e);
                    }
                    else
                    {
                        closeTcs.SetResult(null);
                    }
                    return Task.CompletedTask;
                };

                try
                {
                    await connection.StartAsync().OrTimeout();
                    await connection.InvokeAsync("CallHandlerThatDoesntExist").OrTimeout();
                    await connection.DisposeAsync().OrTimeout();
                    await closeTcs.Task.OrTimeout();
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} during test: {Message}", ex.GetType().Name, ex.Message);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanStreamClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(CanStreamClientMethodFromServer)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 5).OrTimeout();
                    var results = await channel.ReadAllAsync().OrTimeout();

                    Assert.Equal(new[] { 0, 1, 2, 3, 4 }, results.ToArray());
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CanCloseStreamMethodEarly(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(CanCloseStreamMethodEarly)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var cts = new CancellationTokenSource();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 1000, cts.Token).OrTimeout();

                    // Wait for the server to start streaming items
                    await channel.WaitToReadAsync().AsTask().OrTimeout();

                    cts.Cancel();

                    var results = await channel.ReadAllAsync(suppressExceptions: true).OrTimeout();

                    Assert.True(results.Count > 0 && results.Count < 1000);

                    // We should have been canceled.
                    await Assert.ThrowsAsync<TaskCanceledException>(() => channel.Completion);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task StreamDoesNotStartIfTokenAlreadyCanceled(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, LogLevel.Trace, $"{nameof(StreamDoesNotStartIfTokenAlreadyCanceled)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 5, cts.Token).OrTimeout();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        channel.WaitToReadAsync().AsTask().OrTimeout());
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ExceptionFromStreamingSentToClient(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ExceptionFromStreamingSentToClient)}_{protocol.Name}_{transportType}_{path.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(path, transportType, protocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("StreamException").OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("An unexpected error occurred invoking 'StreamException' on the server. InvalidOperationException: Error occurred while streaming.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionIfHubMethodCannotBeResolved(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionIfHubMethodCannotBeResolved)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("!@#$%")).OrTimeout();
                    Assert.Equal("Unknown hub method '!@#$%'", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionOnHubMethodArgumentCountMismatch(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionOnHubMethodArgumentCountMismatch)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Echo", "p1", 42)).OrTimeout();
                    Assert.Equal("Failed to invoke 'Echo' due to an error on the server. InvalidDataException: Invocation provides 2 argument(s) but target expects 1.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionOnHubMethodArgumentTypeMismatch(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionOnHubMethodArgumentTypeMismatch)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Echo", new[] { 42 })).OrTimeout();
                    Assert.StartsWith("Failed to invoke 'Echo' due to an error on the server.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionIfStreamingHubMethodCannotBeResolved(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionIfStreamingHubMethodCannotBeResolved)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("!@#$%");
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Unknown hub method '!@#$%'", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionOnStreamingHubMethodArgumentCountMismatch(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionOnStreamingHubMethodArgumentCountMismatch)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                loggerFactory.AddConsole(LogLevel.Trace);
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 42, 42);
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Failed to invoke 'Stream' due to an error on the server. InvalidDataException: Invocation provides 2 argument(s) but target expects 1.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionOnStreamingHubMethodArgumentTypeMismatch(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionOnStreamingHubMethodArgumentTypeMismatch)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", "xyz");
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Failed to invoke 'Stream' due to an error on the server. InvalidDataException: Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionIfNonStreamMethodInvokedWithStreamAsync(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionIfNonStreamMethodInvokedWithStreamAsync)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("HelloWorld").OrTimeout();
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync()).OrTimeout();
                    Assert.Equal("The client attempted to invoke the non-streaming 'HelloWorld' method with a streaming invocation.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionIfStreamMethodInvokedWithInvoke(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionIfStreamMethodInvokedWithInvoke)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Stream", 3)).OrTimeout();
                    Assert.Equal("The client attempted to invoke the streaming 'Stream' method with a non-streaming invocation.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task ServerThrowsHubExceptionIfBuildingAsyncEnumeratorIsNotPossible(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ServerThrowsHubExceptionIfBuildingAsyncEnumeratorIsNotPossible)}_{hubProtocol.Name}_{transportType}_{hubPath.TrimStart('/')}"))
            {
                var connection = CreateHubConnection(hubPath, transportType, hubProtocol, loggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("StreamBroken").OrTimeout();
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync()).OrTimeout();
                    Assert.Equal("The value returned by the streaming method 'StreamBroken' is not a ChannelReader<>.", ex.Message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        public async Task ClientCanUseJwtBearerTokenForAuthentication(HttpTransportType transportType)
        {
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ClientCanUseJwtBearerTokenForAuthentication)}_{transportType}"))
            {
                async Task<string> AccessTokenProvider()
                {
                    var httpResponse = await new HttpClient().GetAsync(_serverFixture.Url + "/generateJwtToken");
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.ReadAsStringAsync();
                };

                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + "/authorizedhub", transportType, options =>
                    {
                        options.AccessTokenProvider = AccessTokenProvider;
                    })
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();
                    var message = await hubConnection.InvokeAsync<string>(nameof(TestHub.Echo), "Hello, World!").OrTimeout();
                    Assert.Equal("Hello, World!", message);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        public async Task ClientCanSendHeaders(HttpTransportType transportType)
        {
            using (StartVerifableLog(out var loggerFactory, $"{nameof(ClientCanSendHeaders)}_{transportType}"))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + "/default", transportType, options =>
                    {
                        options.Headers["X-test"] = "42";
                        options.Headers["X-42"] = "test";
                    })
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();
                    var headerValues = await hubConnection.InvokeAsync<string[]>(nameof(TestHub.GetHeaderValues), new[] { "X-test", "X-42" }).OrTimeout();
                    Assert.Equal(new[] { "42", "test" }, headerValues);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public async Task WebSocketOptionsAreApplied()
        {
            using (StartVerifableLog(out var loggerFactory, $"{nameof(WebSocketOptionsAreApplied)}"))
            {
                // System.Net has a HttpTransportType type which means we need to fully-qualify this rather than 'use' the namespace
                var cookieJar = new System.Net.CookieContainer();
                cookieJar.Add(new System.Net.Cookie("Foo", "Bar", "/", new Uri(_serverFixture.Url).Host));

                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + "/default", HttpTransportType.WebSockets, options =>
                    {
                        options.WebSocketConfiguration = o => o.Cookies = cookieJar;
                    })
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();
                    var cookieValue = await hubConnection.InvokeAsync<string>(nameof(TestHub.GetCookieValue), "Foo").OrTimeout();
                    Assert.Equal("Bar", cookieValue);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        public async Task CheckHttpConnectionFeatures(HttpTransportType transportType)
        {
            using (StartVerifableLog(out var loggerFactory, $"{nameof(CheckHttpConnectionFeatures)}_{transportType}"))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + "/default", transportType)
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var features = await hubConnection.InvokeAsync<object[]>(nameof(TestHub.GetIHttpConnectionFeatureProperties)).OrTimeout();
                    var localPort = (Int64)features[0];
                    var remotePort = (Int64)features[1];
                    var localIP = (string)features[2];
                    var remoteIP = (string)features[3];

                    Assert.True(localPort > 0L);
                    Assert.True(remotePort > 0L);
                    Assert.Equal("127.0.0.1", localIP);
                    Assert.Equal("127.0.0.1", remoteIP);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Fact]
        public async Task NegotiationSkipsServerSentEventsWhenUsingBinaryProtocol()
        {
            using (StartVerifableLog(out var loggerFactory))
            {
                var hubConnectionBuilder = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .AddMessagePackProtocol()
                    .WithUrl(_serverFixture.Url + "/default-nowebsockets");

                var hubConnection = hubConnectionBuilder.Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var transport = await hubConnection.InvokeAsync<HttpTransportType>(nameof(TestHub.GetActiveTransportName)).OrTimeout();
                    Assert.Equal(HttpTransportType.LongPolling, transport);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Fact]
        public async Task StopCausesPollToReturnImmediately()
        {
            using (StartVerifableLog(out var loggerFactory))
            {
                PollTrackingMessageHandler pollTracker = null;
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(loggerFactory)
                    .WithUrl(_serverFixture.Url + "/default", options =>
                    {
                        options.Transports = HttpTransportType.LongPolling;
                        options.HttpMessageHandlerFactory = handler =>
                        {
                            pollTracker = new PollTrackingMessageHandler(handler);
                            return pollTracker;
                        };
                    })
                    .Build();

                await hubConnection.StartAsync();

                Assert.NotNull(pollTracker);
                Assert.NotNull(pollTracker.ActivePoll);

                var stopTask = hubConnection.StopAsync();

                try
                {
                    // if we completed running before the poll or after the poll started then the task
                    // might complete successfully
                    await pollTracker.ActivePoll.OrTimeout();
                }
                catch (OperationCanceledException)
                {
                    // If this happens it's fine because we were in the middle of a poll
                }

                await stopTask;
            }
        }

        private class PollTrackingMessageHandler : DelegatingHandler
        {
            public Task<HttpResponseMessage> ActivePoll { get; private set; }

            public PollTrackingMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler)
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Method == HttpMethod.Get)
                {
                    ActivePoll = base.SendAsync(request, cancellationToken);
                    return ActivePoll;
                }

                return base.SendAsync(request, cancellationToken);
            }
        }

        public static IEnumerable<object[]> HubProtocolsAndTransportsAndHubPaths
        {
            get
            {
                foreach (var protocol in HubProtocols)
                {
                    foreach (var transport in TransportTypes().SelectMany(t => t).Cast<HttpTransportType>())
                    {
                        foreach (var hubPath in HubPaths)
                        {
                            if (!(protocol.Value is MessagePackHubProtocol) || transport != HttpTransportType.ServerSentEvents)
                            {
                                yield return new object[] { protocol.Key, transport, hubPath };
                            }
                        }
                    }
                }
            }
        }

        // This list excludes "special" hub paths like "default-nowebsockets" which exist for specific tests.
        public static string[] HubPaths = new[] { "/default", "/dynamic", "/hubT" };

        public static Dictionary<string, IHubProtocol> HubProtocols =>
            new Dictionary<string, IHubProtocol>
            {
                { "json", new JsonHubProtocol() },
                { "messagepack", new MessagePackHubProtocol() },
            };

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
}
