// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        public class Extensions
        {
            [Fact]
            public async Task On()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On("Foo",
                        () => tcs.SetResult(new object[0])),
                    new object[0]);
            }

            [Fact]
            public async Task OnT1()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int>("Foo",
                        r => tcs.SetResult(new object[] {r})),
                    new object[] {42});
            }

            [Fact]
            public async Task OnT2()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string>("Foo",
                        (r1, r2) => tcs.SetResult(new object[] {r1, r2})),
                    new object[] {42, "abc"});
            }

            [Fact]
            public async Task OnT3()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float>("Foo",
                        (r1, r2, r3) => tcs.SetResult(new object[] {r1, r2, r3})),
                    new object[] {42, "abc", 24.0f});
            }

            [Fact]
            public async Task OnT4()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float, double>("Foo",
                        (r1, r2, r3, r4) => tcs.SetResult(new object[] {r1, r2, r3, r4})),
                    new object[] {42, "abc", 24.0f, 10d});
            }

            [Fact]
            public async Task OnT5()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float, double, string>("Foo",
                        (r1, r2, r3, r4, r5) => tcs.SetResult(new object[] {r1, r2, r3, r4, r5})),
                    new object[] {42, "abc", 24.0f, 10d, "123"});
            }

            [Fact]
            public async Task OnT6()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float, double, string, byte>("Foo",
                        (r1, r2, r3, r4, r5, r6) => tcs.SetResult(new object[] {r1, r2, r3, r4, r5, r6})),
                    new object[] {42, "abc", 24.0f, 10d, "123", 24});
            }

            [Fact]
            public async Task OnT7()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float, double, string, byte, char>("Foo",
                        (r1, r2, r3, r4, r5, r6, r7) => tcs.SetResult(new object[] {r1, r2, r3, r4, r5, r6, r7})),
                    new object[] {42, "abc", 24.0f, 10d, "123", 24, 'c'});
            }

            [Fact]
            public async Task OnT8()
            {
                await InvokeOn(
                    (hubConnection, tcs) => hubConnection.On<int, string, float, double, string, byte, char, string>("Foo",
                        (r1, r2, r3, r4, r5, r6, r7, r8) => tcs.SetResult(new object[] {r1, r2, r3, r4, r5, r6, r7, r8})),
                    new object[] {42, "abc", 24.0f, 10d, "123", 24, 'c', "XYZ"});
            }

            private async Task InvokeOn(Action<HubConnection, TaskCompletionSource<object[]>> onAction, object[] args)
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                var handlerTcs = new TaskCompletionSource<object[]>();
                try
                {
                    onAction(hubConnection, handlerTcs);
                    await hubConnection.StartAsync();

                    await connection.ReceiveJsonMessage(
                        new
                        {
                            invocationId = "1",
                            type = 1,
                            target = "Foo",
                            arguments = args
                        }).OrTimeout();

                    await handlerTcs.Task.OrTimeout();
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ConnectionNotClosedOnCallbackArgumentCountMismatch()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                var receiveTcs = new TaskCompletionSource<int>();

                try
                {
                    hubConnection.On<int>("Foo", r => { receiveTcs.SetResult(r); });
                    await hubConnection.StartAsync().OrTimeout();

                    await connection.ReceiveJsonMessage(
                        new
                        {
                            invocationId = "1",
                            type = 1,
                            target = "Foo",
                            arguments = new object[] {42, "42"}
                        }).OrTimeout();

                    await connection.ReceiveJsonMessage(
                        new
                        {
                            invocationId = "2",
                            type = 1,
                            target = "Foo",
                            arguments = new object[] {42}
                        }).OrTimeout();

                    Assert.Equal(42, await receiveTcs.Task.OrTimeout());
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task ConnectionNotClosedOnCallbackArgumentTypeMismatch()
            {
                var connection = new TestConnection();
                var hubConnection = CreateHubConnection(connection);
                var receiveTcs = new TaskCompletionSource<int>();

                try
                {
                    hubConnection.On<int>("Foo", r => { receiveTcs.SetResult(r); });
                    await hubConnection.StartAsync().OrTimeout();

                    await connection.ReceiveJsonMessage(
                        new
                        {
                            invocationId = "1",
                            type = 1,
                            target = "Foo",
                            arguments = new object[] {"xxx"}
                        }).OrTimeout();

                    await connection.ReceiveJsonMessage(
                        new
                        {
                            invocationId = "2",
                            type = 1,
                            target = "Foo",
                            arguments = new object[] {42}
                        }).OrTimeout();

                    Assert.Equal(42, await receiveTcs.Task.OrTimeout());
                }
                finally
                {
                    await hubConnection.DisposeAsync().OrTimeout();
                }
            }
        }
    }
}
