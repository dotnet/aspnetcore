// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class HubConnectionTestsCollection : ICollectionFixture<InProcessTestServer<Startup>>
    {
        public const string Name = nameof(HubConnectionTestsCollection);
    }

    [Collection(HubConnectionTestsCollection.Name)]
    public class HubConnectionTests : FunctionalTestBase
    {
        private const string DefaultHubDispatcherLoggerName = "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher";

        private HubConnection CreateHubConnection(
            string url,
            string path = null,
            HttpTransportType? transportType = null,
            IHubProtocol protocol = null,
            ILoggerFactory loggerFactory = null)
        {
            var hubConnectionBuilder = new HubConnectionBuilder();
            hubConnectionBuilder.Services.AddSingleton(protocol);
            hubConnectionBuilder.WithLoggerFactory(loggerFactory);

            var delegateConnectionFactory = new DelegateConnectionFactory(
                GetHttpConnectionFactory(url, loggerFactory, path, transportType ?? HttpTransportType.LongPolling | HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents),
                connection => ((HttpConnection)connection).DisposeAsync());
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            return hubConnectionBuilder.Build();
        }

        private Func<TransferFormat, Task<ConnectionContext>> GetHttpConnectionFactory(string url, ILoggerFactory loggerFactory, string path, HttpTransportType transportType)
        {
            return async format =>
            {
                var connection = new HttpConnection(new Uri(url + path), transportType, loggerFactory);
                await connection.StartAsync(format);
                return connection;
            };
        }

        [Theory]
        [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
        public async Task CheckFixedMessage(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var connectionBuilder = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + path, transportType);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo), originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanStopAndStartConnection(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanStartConnectionFromClosedEvent(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var logger = LoggerFactory.CreateLogger<HubConnectionTests>();
                const string originalMessage = "SignalR";

                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                const string originalMessage = "SignalR";
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var result = await connection.InvokeAsync<string>(nameof(TestHub.Echo).ToLowerInvariant(), originalMessage).OrTimeout();

                    Assert.Equal(originalMessage, result);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanInvokeClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                const string originalMessage = "SignalR";

                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task InvokeNonExistantClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} during test: {Message}", ex.GetType().Name, ex.Message);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanStreamClientMethodFromServer(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 5).OrTimeout();
                    var results = await channel.ReadAllAsync().OrTimeout();

                    Assert.Equal(new[] { 0, 1, 2, 3, 4 }, results.ToArray());
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanStreamToAndFromClientInSameInvocation(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channelWriter = Channel.CreateBounded<string>(5);
                    var channel = await connection.StreamAsChannelAsync<string>("StreamEcho", channelWriter.Reader).OrTimeout();

                    await channelWriter.Writer.WriteAsync("1").AsTask().OrTimeout();
                    Assert.Equal("1", await channel.ReadAsync().AsTask().OrTimeout());
                    await channelWriter.Writer.WriteAsync("2").AsTask().OrTimeout();
                    Assert.Equal("2", await channel.ReadAsync().AsTask().OrTimeout());
                    channelWriter.Writer.Complete();

                    var results = await channel.ReadAllAsync().OrTimeout();
                    Assert.Empty(results);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task CanCloseStreamMethodEarly(string protocolName, HttpTransportType transportType, string path)
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                // Writing is not allowed after writer was completed.
                // Can happen since we are closing the connection while the connection could still be writing the streaming data
                return writeContext.LoggerName == typeof(HubConnectionContext).FullName &&
                       writeContext.EventId.Name == "FailedWritingMessage";
            }

            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        [LogLevel(LogLevel.Trace)]
        public async Task StreamDoesNotStartIfTokenAlreadyCanceled(string protocolName, HttpTransportType transportType, string path)
        {
            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>connection.StreamAsChannelAsync<int>("Stream", 5, cts.Token).OrTimeout());
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var protocol = HubProtocols[protocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("StreamException").OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("An unexpected error occurred invoking 'StreamException' on the server. InvalidOperationException: Error occurred while streaming.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("!@#$%")).OrTimeout();
                    Assert.Equal("Failed to invoke '!@#$%' due to an error on the server. HubException: Method does not exist.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        public async Task ServerThrowsHubExceptionIfHubMethodCannotBeResolvedAndArgumentsPassedIn(string hubProtocolName, HttpTransportType transportType, string hubPath)
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("!@#$%", 10, "test")).OrTimeout();
                    Assert.Equal("Failed to invoke '!@#$%' due to an error on the server. HubException: Method does not exist.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Echo", "p1", 42)).OrTimeout();
                    Assert.Equal("Failed to invoke 'Echo' due to an error on the server. InvalidDataException: Invocation provides 2 argument(s) but target expects 1.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Echo", new[] { 42 })).OrTimeout();
                    Assert.StartsWith("Failed to invoke 'Echo' due to an error on the server.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("!@#$%");
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Failed to invoke '!@#$%' due to an error on the server. HubException: Method does not exist.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", 42, 42);
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Failed to invoke 'Stream' due to an error on the server. InvalidDataException: Invocation provides 2 argument(s) but target expects 1.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "FailedInvokingHubMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var channel = await connection.StreamAsChannelAsync<int>("Stream", "xyz");
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync().OrTimeout());
                    Assert.Equal("Failed to invoke 'Stream' due to an error on the server. InvalidDataException: Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "NonStreamingMethodCalledWithStream";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("HelloWorld").OrTimeout();
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync()).OrTimeout();
                    Assert.Equal("The client attempted to invoke the non-streaming 'HelloWorld' method with a streaming invocation.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "StreamingMethodCalledWithInvoke";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();

                    var ex = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Stream", 3)).OrTimeout();
                    Assert.Equal("The client attempted to invoke the streaming 'Stream' method with a non-streaming invocation.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            bool ExpectedErrors(WriteContext writeContext)
            {
                return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                       writeContext.EventId.Name == "InvalidReturnValueFromStreamingMethod";
            }

            var hubProtocol = HubProtocols[hubProtocolName];
            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                try
                {
                    await connection.StartAsync().OrTimeout();
                    var channel = await connection.StreamAsChannelAsync<int>("StreamBroken").OrTimeout();
                    var ex = await Assert.ThrowsAsync<HubException>(() => channel.ReadAllAsync()).OrTimeout();
                    Assert.Equal("The value returned by the streaming method 'StreamBroken' is not a ChannelReader<>.", ex.Message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await connection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Fact]
        public async Task RandomGenericIsNotTreatedAsStream()
        {
            bool ExpectedErrors(WriteContext writeContext)
            {
                return "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" == writeContext.LoggerName &&
                    "FailedInvokingHubMethod" == writeContext.EventId.Name;
            }
            var hubPath = HubPaths[0];
            var hubProtocol = HubProtocols.First().Value;
            var transportType = TransportTypes().First().Cast<HttpTransportType>().First();

            using (StartServer<Startup>(out var server, ExpectedErrors))
            {
                var connection = CreateHubConnection(server.Url, hubPath, transportType, hubProtocol, LoggerFactory);
                await connection.StartAsync().OrTimeout();
                // List<T> will be looked at to replace with a StreamPlaceholder and should be skipped, so an error will be thrown from the
                // protocol on the server when it tries to match List<T> with a StreamPlaceholder
                var hubException = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<int>("StreamEcho", new List<string> { "1", "2" }).OrTimeout());
                Assert.Equal("Failed to invoke 'StreamEcho' due to an error on the server. InvalidDataException: Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.",
                    hubException.Message);
                await connection.DisposeAsync().OrTimeout();
            }
        }

        [Theory]
        [MemberData(nameof(TransportTypes))]
        public async Task ClientCanUseJwtBearerTokenForAuthentication(HttpTransportType transportType)
        {
            using (StartServer<Startup>(out var server))
            {
                async Task<string> AccessTokenProvider()
                {
                    var httpResponse = await new HttpClient().GetAsync(server.Url + "/generateJwtToken");
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.ReadAsStringAsync();
                };

                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/authorizedhub", transportType, options =>
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
        public async Task ClientCanUseJwtBearerTokenForAuthenticationWhenRedirected(HttpTransportType transportType)
        {
            using (StartServer<Startup>(out var server))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/redirect", transportType)
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();
                    var message = await hubConnection.InvokeAsync<string>(nameof(TestHub.Echo), "Hello, World!").OrTimeout();
                    Assert.Equal("Hello, World!", message);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/default", transportType, options =>
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                // System.Net has a HttpTransportType type which means we need to fully-qualify this rather than 'use' the namespace
                var cookieJar = new System.Net.CookieContainer();
                cookieJar.Add(new System.Net.Cookie("Foo", "Bar", "/", new Uri(server.Url).Host));

                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/default", HttpTransportType.WebSockets, options =>
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
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Fact]
        public async Task CheckHttpConnectionFeatures()
        {
            using (StartServer<Startup>(out var server))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/default")
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var features = await hubConnection.InvokeAsync<object[]>(nameof(TestHub.GetIHttpConnectionFeatureProperties)).OrTimeout();
                    var localPort = (long)features[0];
                    var remotePort = (long)features[1];
                    var localIP = (string)features[2];
                    var remoteIP = (string)features[3];

                    Assert.True(localPort > 0L);
                    Assert.True(remotePort > 0L);
                    Assert.Equal("127.0.0.1", localIP);
                    Assert.Equal("127.0.0.1", remoteIP);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                    throw;
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }

        [Fact]
        public async Task UserIdProviderCanAccessHttpContext()
        {
            using (StartServer<Startup>(out var server))
            {
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/default", options =>
                    {
                        options.Headers.Add(HeaderUserIdProvider.HeaderName, "SuperAdmin");
                    })
                    .Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var userIdentifier = await hubConnection.InvokeAsync<string>(nameof(TestHub.GetUserIdentifier)).OrTimeout();
                    Assert.Equal("SuperAdmin", userIdentifier);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                var hubConnectionBuilder = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .AddMessagePackProtocol()
                    .WithUrl(server.Url + "/default-nowebsockets");

                var hubConnection = hubConnectionBuilder.Build();
                try
                {
                    await hubConnection.StartAsync().OrTimeout();

                    var transport = await hubConnection.InvokeAsync<HttpTransportType>(nameof(TestHub.GetActiveTransportName)).OrTimeout();
                    Assert.Equal(HttpTransportType.LongPolling, transport);
                }
                catch (Exception ex)
                {
                    LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
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
            using (StartServer<Startup>(out var server))
            {
                PollTrackingMessageHandler pollTracker = null;
                var hubConnection = new HubConnectionBuilder()
                    .WithLoggerFactory(LoggerFactory)
                    .WithUrl(server.Url + "/default", options =>
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
                { "json", new NewtonsoftJsonHubProtocol() },
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
