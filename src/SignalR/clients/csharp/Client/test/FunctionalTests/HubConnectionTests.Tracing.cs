// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Test.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

partial class HubConnectionTests : FunctionalTestBase
{
    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    public async Task InvokeAsync_SendTraceHeader(string protocolName, HttpTransportType transportType, string path)
    {
        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientChannel = Channel.CreateUnbounded<Activity>();
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientChannel.Writer.TryWrite(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + path, transportType);
            connectionBuilder.Services.AddSingleton(protocol);
            connectionBuilder.Services.AddSingleton(clientSourceContainer);

            var connection = connectionBuilder.Build();

            Activity clientParentActivity1 = null;
            Activity clientActivity1 = null;
            Activity clientParentActivity2 = null;
            Activity clientActivity2 = null;
            try
            {
                await connection.StartAsync().DefaultTimeout();

                // Invocation 1
                try
                {
                    clientParentActivity1 = new Activity("ClientActivity1");
                    clientParentActivity1.AddBaggage("baggage-1", "value-1");
                    clientParentActivity1.Start();

                    var resultTask = connection.InvokeAsync<string>(nameof(TestHub.HelloWorld)).DefaultTimeout();

                    clientActivity1 = await clientChannel.Reader.ReadAsync().DefaultTimeout();

                    // The SignalR client activity shouldn't escape into user code.
                    Assert.Equal(clientParentActivity1, Activity.Current);

                    var result = await resultTask.ConfigureAwait(false);
                    Assert.Equal("Hello World!", result);
                }
                finally
                {
                    clientParentActivity1?.Stop();
                }

                // Invocation 2
                try
                {
                    clientParentActivity2 = new Activity("ClientActivity2");
                    clientParentActivity2.AddBaggage("baggage-2", "value-2");
                    clientParentActivity2.Start();

                    var resultTask = connection.InvokeAsync<string>(nameof(TestHub.HelloWorld));

                    clientActivity2 = await clientChannel.Reader.ReadAsync().DefaultTimeout();

                    // The SignalR client activity shouldn't escape into user code.
                    Assert.Equal(clientParentActivity2, Activity.Current);

                    var result = await resultTask.DefaultTimeout();
                    Assert.Equal("Hello World!", result);
                }
                finally
                {
                    clientParentActivity2?.Stop();
                }
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

            var port = new Uri(server.Url).Port;
            var serverHubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 4).DefaultTimeout();

            Assert.Collection(serverActivities,
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.OnConnected, a.OperationName);
                    Assert.Equal($"{serverHubName}/OnConnectedAsync", a.DisplayName);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", a.Parent.OperationName);
                    Assert.Equal(ActivityKind.Internal, a.Kind);
                    Assert.False(a.HasRemoteParent);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);
                    Assert.Empty(a.Baggage);
                },
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.InvocationIn, a.OperationName);
                    Assert.Equal($"{serverHubName}/HelloWorld", a.DisplayName);
                    Assert.Equal(clientActivity1.Id, a.ParentId);
                    Assert.Equal(ActivityKind.Server, a.Kind);
                    Assert.True(a.HasRemoteParent);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);

                    var baggage = a.Baggage.ToDictionary();
                    Assert.Equal("value-1", baggage["baggage-1"]);
                },
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.InvocationIn, a.OperationName);
                    Assert.Equal($"{serverHubName}/HelloWorld", a.DisplayName);
                    Assert.Equal(clientActivity2.Id, a.ParentId);
                    Assert.Equal(ActivityKind.Server, a.Kind);
                    Assert.True(a.HasRemoteParent);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);

                    var baggage = a.Baggage.ToDictionary();
                    Assert.Equal("value-2", baggage["baggage-2"]);
                },
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.OnDisconnected, a.OperationName);
                    Assert.Equal($"{serverHubName}/OnDisconnectedAsync", a.DisplayName);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", a.Parent.OperationName);
                    Assert.Equal(ActivityKind.Internal, a.Kind);
                    Assert.False(a.HasRemoteParent);
                    Assert.Empty(a.Baggage);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);
                });

            // Client activity 1
            Assert.Equal(HubConnection.ActivityName, clientActivity1.OperationName);
            Assert.Equal($"{clientHubName}/HelloWorld", clientActivity1.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity1.Kind);
            Assert.Equal(clientParentActivity1, clientActivity1.Parent);
            Assert.Equal(ActivityStatusCode.Unset, clientActivity1.Status);

            var baggage = clientActivity1.Baggage.ToDictionary();
            Assert.Equal("value-1", baggage["baggage-1"]);

            var tags = clientActivity1.TagObjects.ToDictionary();
            Assert.Equal("signalr", tags["rpc.system"]);
            Assert.Equal("HelloWorld", tags["rpc.method"]);
            Assert.Equal(clientHubName, tags["rpc.service"]);
            Assert.Equal("127.0.0.1", tags["server.address"]);
            Assert.Equal(port, (int)tags["server.port"]);

            // Client activity 2
            Assert.Equal(HubConnection.ActivityName, clientActivity2.OperationName);
            Assert.Equal($"{clientHubName}/HelloWorld", clientActivity2.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity2.Kind);
            Assert.Equal(clientParentActivity2, clientActivity2.Parent);
            Assert.Equal(ActivityStatusCode.Unset, clientActivity2.Status);

            baggage = clientActivity2.Baggage.ToDictionary();
            Assert.Equal("value-2", baggage["baggage-2"]);

            tags = clientActivity2.TagObjects.ToDictionary();
            Assert.Equal("signalr", tags["rpc.system"]);
            Assert.Equal("HelloWorld", tags["rpc.method"]);
            Assert.Equal(clientHubName, tags["rpc.service"]);
            Assert.Equal("127.0.0.1", tags["server.address"]);
            Assert.Equal(port, (int)tags["server.port"]);
        }
    }

    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    public async Task StreamAsyncCore_SendTraceHeader(string protocolName, HttpTransportType transportType, string path)
    {
        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientActivityTcs = new TaskCompletionSource<Activity>();
            Activity clientActivity = null;
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientActivityTcs.SetResult(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory, activitySourceContainer: clientSourceContainer);

            Activity clientParentActivity = null;
            try
            {
                await connection.StartAsync().DefaultTimeout();

                clientParentActivity = new Activity("ClientActivity");
                clientParentActivity.AddBaggage("baggage-1", "value-1");
                clientParentActivity.Start();

                var expectedValue = 0;
                var streamTo = 5;
                var asyncEnumerable = connection.StreamAsyncCore<int>("Stream", new object[] { streamTo });

                await foreach (var streamValue in asyncEnumerable)
                {
                    // Call starts after user reads from the enumerable.
                    if (streamValue == 0)
                    {
                        // The SignalR client activity should be:
                        // - Started
                        // - Still running
                        // - Not escaped into user code
                        clientActivity = await clientActivityTcs.Task.DefaultTimeout();
                        Assert.NotNull(clientActivity);
                        Assert.False(clientActivity.IsStopped);
                        Assert.Equal(clientParentActivity, Activity.Current);
                    }

                    Assert.Equal(expectedValue, streamValue);
                    expectedValue++;
                }

                Assert.Equal(streamTo, expectedValue);
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<HubConnectionTests>().LogError(ex, "{ExceptionType} from test", ex.GetType().FullName);
                throw;
            }
            finally
            {
                clientParentActivity?.Stop();
                await connection.DisposeAsync().DefaultTimeout();
            }

            var port = new Uri(server.Url).Port;
            var hubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 3).DefaultTimeout();

            Assert.Collection(serverActivities,
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.OnConnected, a.OperationName);
                    Assert.Equal($"{hubName}/OnConnectedAsync", a.DisplayName);
                    Assert.Equal(ActivityKind.Internal, a.Kind);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", a.Parent.OperationName);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);
                    Assert.False(a.HasRemoteParent);
                    Assert.Empty(a.Baggage);
                },
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.InvocationIn, a.OperationName);
                    Assert.Equal($"{hubName}/Stream", a.DisplayName);
                    Assert.Equal(ActivityKind.Server, a.Kind);
                    Assert.Equal(clientActivity.Id, a.ParentId);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);
                    Assert.True(a.HasRemoteParent);
                    Assert.True(a.IsStopped);

                    var baggage = a.Baggage.ToDictionary();
                    Assert.Equal("value-1", baggage["baggage-1"]);
                },
                a =>
                {
                    Assert.Equal(SignalRServerActivitySource.OnDisconnected, a.OperationName);
                    Assert.Equal($"{hubName}/OnDisconnectedAsync", a.DisplayName);
                    Assert.Equal(ActivityKind.Internal, a.Kind);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", a.Parent.OperationName);
                    Assert.Equal(ActivityStatusCode.Unset, a.Status);
                    Assert.False(a.HasRemoteParent);
                    Assert.Empty(a.Baggage);
                });

            Assert.Equal(HubConnection.ActivityName, clientActivity.OperationName);
            Assert.Equal($"{clientHubName}/Stream", clientActivity.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity.Kind);
            Assert.Equal(clientParentActivity, clientActivity.Parent);
            Assert.Equal(ActivityStatusCode.Unset, clientActivity.Status);
            Assert.True(clientActivity.IsStopped);

            var baggage = clientActivity.Baggage.ToDictionary();
            Assert.Equal("value-1", baggage["baggage-1"]);

            var tags = clientActivity.TagObjects.ToDictionary();
            Assert.Equal("signalr", tags["rpc.system"]);
            Assert.Equal("Stream", tags["rpc.method"]);
            Assert.Equal(clientHubName, tags["rpc.service"]);
            Assert.Equal("127.0.0.1", tags["server.address"]);
            Assert.Equal(port, (int)tags["server.port"]);
        }
    }

    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    [LogLevel(LogLevel.Trace)]
    public async Task StreamAsyncCanBeCanceled_Tracing(string protocolName, HttpTransportType transportType, string path)
    {
        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientActivityTcs = new TaskCompletionSource<Activity>();
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientActivityTcs.SetResult(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory, activitySourceContainer: clientSourceContainer);
            try
            {
                await connection.StartAsync().DefaultTimeout();

                var cts = new CancellationTokenSource();

                var stream = connection.StreamAsync<int>("Stream", 1000, cts.Token);
                var results = new List<int>();

                var enumerator = stream.GetAsyncEnumerator();
                await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    while (await enumerator.MoveNextAsync())
                    {
                        results.Add(enumerator.Current);
                        cts.Cancel();
                    }
                });

                Assert.True(results.Count > 0 && results.Count < 1000);
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

            var port = new Uri(server.Url).Port;
            var hubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 3).DefaultTimeout();
            var clientActivity = await clientActivityTcs.Task.DefaultTimeout();

            Assert.Collection(serverActivities,
                a => Assert.Equal($"{hubName}/OnConnectedAsync", a.DisplayName),
                a =>
                {
                    Assert.Equal($"{hubName}/Stream", a.DisplayName);
                    Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

                    var tags = clientActivity.TagObjects.ToDictionary();
                    Assert.Equal(typeof(OperationCanceledException).FullName, tags["error.type"]);
                },
                a => Assert.Equal($"{hubName}/OnDisconnectedAsync", a.DisplayName));

            Assert.Equal($"{clientHubName}/Stream", clientActivity.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity.Kind);
            Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

            var tags = clientActivity.TagObjects.ToDictionary();
            Assert.Equal(typeof(OperationCanceledException).FullName, tags["error.type"]);
        }
    }

    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    [LogLevel(LogLevel.Trace)]
    public async Task StreamAsyncWithException_Tracing(string protocolName, HttpTransportType transportType, string path)
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                   writeContext.EventId.Name == "FailedInvokingHubMethod";
        }

        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientActivityTcs = new TaskCompletionSource<Activity>();
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientActivityTcs.SetResult(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connection = CreateHubConnection(server.Url, path, transportType, protocol, LoggerFactory, activitySourceContainer: clientSourceContainer);
            try
            {
                await connection.StartAsync().DefaultTimeout();
                var asyncEnumerable = connection.StreamAsync<int>("StreamException");
                var ex = await Assert.ThrowsAsync<HubException>(async () =>
                {
                    await foreach (var streamValue in asyncEnumerable)
                    {
                        Assert.Fail("Expected an exception from the streaming invocation.");
                    }
                });

                Assert.Equal("An unexpected error occurred invoking 'StreamException' on the server. InvalidOperationException: Error occurred while streaming.", ex.Message);

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

            var port = new Uri(server.Url).Port;
            var serverHubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 3).DefaultTimeout();
            var clientActivity = await clientActivityTcs.Task.DefaultTimeout();

            Assert.Collection(serverActivities,
                a => Assert.Equal($"{serverHubName}/OnConnectedAsync", a.DisplayName),
                a =>
                {
                    Assert.Equal($"{serverHubName}/StreamException", a.DisplayName);
                    Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

                    var tags = clientActivity.TagObjects.ToDictionary();
                    Assert.Equal(typeof(HubException).FullName, tags["error.type"]);
                },
                a => Assert.Equal($"{serverHubName}/OnDisconnectedAsync", a.DisplayName));

            Assert.Equal($"{clientHubName}/StreamException", clientActivity.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity.Kind);
            Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

            var tags = clientActivity.TagObjects.ToDictionary();
            Assert.Equal(typeof(HubException).FullName, tags["error.type"]);
        }
    }

    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    public async Task InvokeAsyncWithException_Tracing(string protocolName, HttpTransportType transportType, string path)
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == DefaultHubDispatcherLoggerName &&
                   writeContext.EventId.Name == "FailedInvokingHubMethod";
        }

        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>(ExpectedErrors))
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientActivityTcs = new TaskCompletionSource<Activity>();
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientActivityTcs.SetResult(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + path, transportType);
            connectionBuilder.Services.AddSingleton(protocol);
            connectionBuilder.Services.AddSingleton(clientSourceContainer);

            var connection = connectionBuilder.Build();

            try
            {
                await connection.StartAsync().DefaultTimeout();

                await Assert.ThrowsAnyAsync<Exception>(
                    async () => await connection.InvokeAsync<string>(nameof(TestHub.InvokeException))).DefaultTimeout();
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

            var port = new Uri(server.Url).Port;
            var serverHubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 3).DefaultTimeout();
            var clientActivity = await clientActivityTcs.Task.DefaultTimeout();

            Assert.Collection(serverActivities,
                a => Assert.Equal($"{serverHubName}/OnConnectedAsync", a.DisplayName),
                a =>
                {
                    Assert.Equal($"{serverHubName}/InvokeException", a.DisplayName);
                    Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

                    var tags = clientActivity.TagObjects.ToDictionary();
                    Assert.Equal(typeof(HubException).FullName, tags["error.type"]);
                },
                a => Assert.Equal($"{serverHubName}/OnDisconnectedAsync", a.DisplayName));

            Assert.Equal(HubConnection.ActivityName, clientActivity.OperationName);
            Assert.Equal($"{clientHubName}/InvokeException", clientActivity.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity.Kind);
            Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);

            var tags = clientActivity.TagObjects.ToDictionary();
            Assert.Equal(typeof(HubException).FullName, tags["error.type"]);
        }
    }

    [Theory]
    [MemberData(nameof(HubProtocolsAndTransportsAndHubPaths))]
    public async Task SendAsync_Tracing(string protocolName, HttpTransportType transportType, string path)
    {
        var protocol = HubProtocols[protocolName];
        await using (var server = await StartServer<Startup>())
        {
            var serverChannel = Channel.CreateUnbounded<Activity>();
            var clientActivityTcs = new TaskCompletionSource<Activity>();
            var serverSource = server.Services.GetRequiredService<SignalRServerActivitySource>().ActivitySource;
            var clientSourceContainer = new SignalRClientActivitySource();

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, serverSource) || ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity =>
                {
                    if (activity.Source == clientSourceContainer.ActivitySource)
                    {
                        clientActivityTcs.SetResult(activity);
                    }
                    else
                    {
                        serverChannel.Writer.TryWrite(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            var connectionBuilder = new HubConnectionBuilder()
                .WithLoggerFactory(LoggerFactory)
                .WithUrl(server.Url + path, transportType);
            connectionBuilder.Services.AddSingleton(protocol);
            connectionBuilder.Services.AddSingleton(clientSourceContainer);

            var connection = connectionBuilder.Build();

            try
            {
                await connection.StartAsync().DefaultTimeout();

                var echoTcs = new TaskCompletionSource<string>();
                connection.On<string>("Echo", echoTcs.SetResult);

                await connection.SendAsync(nameof(TestHub.CallEcho), "Hi");

                // The SignalR client activity shouldn't escape into user code.
                Assert.Null(Activity.Current);

                // Wait until message is echoed back from the server.
                // Needed so the client doesn't stop the connection before the server gets the invocation.
                await echoTcs.Task.DefaultTimeout();
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

            var port = new Uri(server.Url).Port;
            var serverHubName = path switch
            {
                "/default" => typeof(TestHub).FullName,
                "/hubT" => typeof(TestHubT).FullName,
                "/dynamic" => typeof(DynamicTestHub).FullName,
                _ => throw new InvalidOperationException("Unexpected path: " + path)
            };
            var clientHubName = path.TrimStart('/');

            var serverActivities = await serverChannel.Reader.ReadAtLeastAsync(minimumCount: 3).DefaultTimeout();
            var clientActivity = await clientActivityTcs.Task.DefaultTimeout();

            Assert.Collection(serverActivities,
                a => Assert.Equal($"{serverHubName}/OnConnectedAsync", a.DisplayName),
                a =>
                {
                    Assert.Equal($"{serverHubName}/CallEcho", a.DisplayName);
                    Assert.Equal(clientActivity.Id, a.ParentId);
                },
                a => Assert.Equal($"{serverHubName}/OnDisconnectedAsync", a.DisplayName));

            Assert.Equal(HubConnection.ActivityName, clientActivity.OperationName);
            Assert.Equal($"{clientHubName}/CallEcho", clientActivity.DisplayName);
            Assert.Equal(ActivityKind.Client, clientActivity.Kind);
        }
    }
}
