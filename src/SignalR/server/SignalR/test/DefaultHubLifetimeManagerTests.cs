// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Specification.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultHubLifetimeManagerTests : HubLifetimeManagerTestsBase<MyHub>
    {
        public override HubLifetimeManager<MyHub> CreateNewHubLifetimeManager()
        {
            return new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
        }

        [Fact]
        public async Task SendAllAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendAllAsync("Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection2.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
                Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task SendAllExceptAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendAllExceptAsync("Hello", new object[] { "World" }, new List<string> { connection1.ConnectionId }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection2.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
                Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
                Assert.Null(client1.TryRead());
            }
        }

        [Fact]
        public async Task SendConnectionAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendConnectionAsync(connection1.ConnectionId, "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection1.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
            }
        }

        [Fact]
        public async Task SendConnectionsAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendConnectionsAsync(new List<string> { connection1.ConnectionId }, "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection1.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
            }
        }

        [Fact]
        public async Task SendGroupAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendGroupAsync("group", "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection1.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
            }
        }

        [Fact]
        public async Task SendGroupExceptAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                await manager.AddToGroupAsync(connection2.ConnectionId, "group").OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendGroupExceptAsync("group", "Hello", new object[] { "World" }, new List<string> { connection1.ConnectionId }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection2.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
                Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
                Assert.Null(client1.TryRead());
            }
        }

        [Fact]
        public async Task SendGroupsAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendGroupsAsync(new List<string> { "group" }, "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection1.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
            }
        }

        [Fact]
        public async Task SendUserAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "user");
                var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "user");
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendUserAsync("user", "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection2.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
                Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task SendUsersAsyncWillCancelWithToken()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient(pauseWriterThreshold: 2))
            {
                var manager = CreateNewHubLifetimeManager();
                var connection1 = HubConnectionContextUtils.Create(client1.Connection, userIdentifier: "user1");
                var connection2 = HubConnectionContextUtils.Create(client2.Connection, userIdentifier: "user2");
                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();
                var cts = new CancellationTokenSource();
                var sendTask = manager.SendUsersAsync(new List<string> { "user1", "user2" }, "Hello", new object[] { "World" }, cts.Token).OrTimeout();
                Assert.False(sendTask.IsCompleted);
                cts.Cancel();
                await sendTask.OrTimeout();
                var message = Assert.IsType<InvocationMessage>(client1.TryRead());
                Assert.Equal("Hello", message.Target);
                Assert.Single(message.Arguments);
                Assert.Equal("World", (string)message.Arguments[0]);
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                connection2.ConnectionAborted.Register(t =>
                {
                    ((TaskCompletionSource<object>)t).SetResult(null);
                }, tcs);
                await tcs.Task.OrTimeout();
                Assert.False(connection1.ConnectionAborted.IsCancellationRequested);
            }
        }
    }
}
