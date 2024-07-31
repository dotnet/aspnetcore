// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

    }
}
