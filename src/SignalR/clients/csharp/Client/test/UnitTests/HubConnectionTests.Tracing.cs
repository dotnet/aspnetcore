// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HubConnectionTests
{
    public class Tracing : VerifiableLoggedTest
    {
        [Fact]
        public async Task InvokeSendsAnInvocationMessage_SendTraceHeaders()
        {
            var clientSourceContainer = new SignalRClientActivitySource();
            Activity clientActivity = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => clientActivity = activity
            };
            ActivitySource.AddActivityListener(listener);

            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, clientActivitySource: clientSourceContainer);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var invokeTask = hubConnection.InvokeAsync("Foo");

                var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
                var traceParent = (string)invokeMessage["headers"]["traceparent"];

                Assert.Equal(clientActivity.Id, traceParent);
                Assert.Equal("example.com", clientActivity.TagObjects.Single(t => t.Key == "server.address").Value);

                Assert.Equal(TaskStatus.WaitingForActivation, invokeTask.Status);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task StreamSendsAnInvocationMessage_SendTraceHeaders()
        {
            var clientSourceContainer = new SignalRClientActivitySource();
            Activity clientActivity = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => clientActivity = activity
            };
            ActivitySource.AddActivityListener(listener);

            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, clientActivitySource: clientSourceContainer);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                var channel = await hubConnection.StreamAsChannelAsync<object>("Foo").DefaultTimeout();

                var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
                var traceParent = (string)invokeMessage["headers"]["traceparent"];

                Assert.Equal(clientActivity.Id, traceParent);

                // Complete the channel
                await connection.ReceiveJsonMessage(new { invocationId = "1", type = 3 }).DefaultTimeout();
                await channel.Completion.DefaultTimeout();
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task SendAnInvocationMessage_SendTraceHeaders()
        {
            var clientSourceContainer = new SignalRClientActivitySource();
            Activity clientActivity = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => clientActivity = activity
            };
            ActivitySource.AddActivityListener(listener);

            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, clientActivitySource: clientSourceContainer);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                await hubConnection.SendAsync("Foo");

                var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
                var traceParent = (string)invokeMessage["headers"]["traceparent"];

                Assert.Equal(clientActivity.Id, traceParent);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeSendsAnInvocationMessage_ConnectionRemoteEndPointChanged_UseRemoteEndpointUrl()
        {
            var clientSourceContainer = new SignalRClientActivitySource();
            Activity clientActivity = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => clientActivity = activity
            };
            ActivitySource.AddActivityListener(listener);

            TestConnection connection = null;
            connection = new TestConnection(onStart: () =>
            {
                connection.RemoteEndPoint = new UriEndPoint(new Uri("http://example.net"));
                return Task.CompletedTask;
            });
            var hubConnection = CreateHubConnection(connection, clientActivitySource: clientSourceContainer);
            try
            {
                await hubConnection.StartAsync().DefaultTimeout();

                _ = hubConnection.InvokeAsync("Foo");

                await connection.ReadSentJsonAsync().DefaultTimeout();

                Assert.Equal("example.net", clientActivity.TagObjects.Single(t => t.Key == "server.address").Value);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }

        [Fact]
        public async Task InvokeSendsAnInvocationMessage_ConnectionRemoteEndPointChangedDuringConnect_UseRemoteEndpointUrl()
        {
            var clientSourceContainer = new SignalRClientActivitySource();
            var clientActivityTcs = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously); ;

            using var listener = new ActivityListener
            {
                ShouldListenTo = activitySource => ReferenceEquals(activitySource, clientSourceContainer.ActivitySource),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = clientActivityTcs.SetResult
            };
            ActivitySource.AddActivityListener(listener);

            var syncPoint = new SyncPoint();
            TestConnection connection = null;
            connection = new TestConnection(onStart: async () =>
            {
                await syncPoint.WaitToContinue();
                connection.RemoteEndPoint = new UriEndPoint(new Uri("http://example.net:5050"));
            });
            var hubConnection = CreateHubConnection(connection, clientActivitySource: clientSourceContainer);
            try
            {
                var startTask = hubConnection.StartAsync();

                _ = hubConnection.InvokeAsync("Foo");

                var clientActivity = await clientActivityTcs.Task.DefaultTimeout();

                // Initial server.address uses configured HubConnection URL.
                Assert.Equal("example.com", clientActivity.TagObjects.Single(t => t.Key == "server.address").Value);
                Assert.Equal(80, (int)clientActivity.TagObjects.Single(t => t.Key == "server.port").Value);

                syncPoint.Continue();

                await startTask.DefaultTimeout();

                await connection.ReadSentJsonAsync().DefaultTimeout();

                // After connection is started, server.address is updated to the connection's remote endpoint.
                Assert.Equal("example.net", clientActivity.TagObjects.Single(t => t.Key == "server.address").Value);
                Assert.Equal(5050, (int)clientActivity.TagObjects.Single(t => t.Key == "server.port").Value);
            }
            finally
            {
                await hubConnection.DisposeAsync().DefaultTimeout();
                await connection.DisposeAsync().DefaultTimeout();
            }
        }
    }
}
