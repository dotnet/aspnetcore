// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HubConnectionTests : VerifiableLoggedTest
{
    [Fact]
    public async Task InvokeThrowsIfSerializingMessageFails()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   writeContext.EventId.Name == "FailedToSendInvocation";
        }
        using (StartVerifiableLog(ExpectedErrors))
        {
            var exception = new InvalidOperationException();
            var hubConnection = CreateHubConnection(new TestConnection(), protocol: MockHubProtocol.Throw(exception), LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.InvokeAsync<int>("test").DefaultTimeout());
            Assert.Same(exception, actualException);
        }
    }

    [Fact]
    public async Task SendAsyncThrowsIfSerializingMessageFails()
    {
        using (StartVerifiableLog())
        {
            var exception = new InvalidOperationException();
            var hubConnection = CreateHubConnection(new TestConnection(), protocol: MockHubProtocol.Throw(exception), LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var actualException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await hubConnection.SendAsync("test").DefaultTimeout());
            Assert.Same(exception, actualException);
        }
    }

    [Fact]
    public async Task ClosedEventRaisedWhenTheClientIsStopped()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://example.com");

        var delegateConnectionFactory = new DelegateConnectionFactory(
            endPoint => new TestConnection().StartAsync());
        builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

        var hubConnection = builder.Build();
        var closedEventTcs = new TaskCompletionSource<Exception>();
        hubConnection.Closed += e =>
        {
            closedEventTcs.SetResult(e);
            return Task.CompletedTask;
        };

        await hubConnection.StartAsync().DefaultTimeout();
        await hubConnection.StopAsync().DefaultTimeout();
        Assert.Null(await closedEventTcs.Task);
    }

    [Fact]
    public async Task StopAsyncCanBeCalledFromOnHandler()
    {
        var connection = new TestConnection();
        var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.On("method", async () =>
        {
            await hubConnection.StopAsync().DefaultTimeout();
            tcs.SetResult();
        });

        await hubConnection.StartAsync().DefaultTimeout();

        await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.InvocationMessageType, target = "method", arguments = new object[] { } }).DefaultTimeout();

        await tcs.Task.DefaultTimeout();
    }

    [Fact]
    public async Task StopAsyncDoesNotWaitForOnHandlers()
    {
        var connection = new TestConnection();
        var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var methodCalledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.On("method", async () =>
        {
            methodCalledTcs.SetResult();
            await tcs.Task;
        });

        await hubConnection.StartAsync().DefaultTimeout();

        await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.InvocationMessageType, target = "method", arguments = new object[] { } }).DefaultTimeout();

        await methodCalledTcs.Task.DefaultTimeout();
        await hubConnection.StopAsync().DefaultTimeout();

        tcs.SetResult();
    }

    [Fact]
    public async Task PendingInvocationsAreCanceledWhenConnectionClosesCleanly()
    {
        using (StartVerifiableLog())
        {
            var hubConnection = CreateHubConnection(new TestConnection(), loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod").DefaultTimeout();
            await hubConnection.StopAsync().DefaultTimeout();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }
    }

    [Fact]
    public async Task PendingInvocationsAreTerminatedWithExceptionWhenTransportCompletesWithError()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   (writeContext.EventId.Name == "ShutdownWithError" ||
                   writeContext.EventId.Name == "ServerDisconnectedWithError");
        }
        using (StartVerifiableLog(ExpectedErrors))
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, protocol: Mock.Of<IHubProtocol>(), LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod").DefaultTimeout();

            var exception = new InvalidOperationException();
            connection.CompleteFromTransport(exception);

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await invokeTask);
            Assert.Equal(exception, actualException);
        }
    }

    [Fact]
    public async Task PendingInvocationsAreCanceledWhenTokenTriggered()
    {
        using (StartVerifiableLog())
        {
            var hubConnection = CreateHubConnection(new TestConnection(), loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            var cts = new CancellationTokenSource();
            var invokeTask = hubConnection.InvokeAsync<int>("testMethod", cancellationToken: cts.Token).DefaultTimeout();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await invokeTask);
        }
    }

    [Fact]
    public async Task InvokeAsyncCanceledWhenPassedCanceledToken()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                hubConnection.InvokeAsync<int>("testMethod", cancellationToken: new CancellationToken(canceled: true)).DefaultTimeout());

            await hubConnection.StopAsync().DefaultTimeout();

            // Assert that InvokeAsync didn't send a message
            Assert.Equal("{\"type\":7}", await connection.ReadSentTextMessageAsync().DefaultTimeout());
        }
    }

    [Fact]
    public async Task SendAsyncCanceledWhenPassedCanceledToken()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                hubConnection.SendAsync("testMethod", cancellationToken: new CancellationToken(canceled: true)).DefaultTimeout());

            await hubConnection.StopAsync().DefaultTimeout();

            // Assert that SendAsync didn't send a message
            Assert.Equal("{\"type\":7}", await connection.ReadSentTextMessageAsync().DefaultTimeout());
        }
    }

    [Fact]
    public async Task SendAsyncCanceledWhenTokenCanceledDuringSend()
    {
        using (StartVerifiableLog())
        {
            // Use pause threshold to block FlushAsync when writing 100+ bytes
            var connection = new TestConnection(pipeOptions: new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, pauseWriterThreshold: 100, useSynchronizationContext: false, resumeWriterThreshold: 50));
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            // Send 100+ bytes to trigger pauseWriterThreshold
            var sendTask = hubConnection.SendAsync("testMethod", new byte[100], cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => sendTask.DefaultTimeout());

            await hubConnection.StopAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task StreamAsChannelAsyncCanceledWhenPassedCanceledToken()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                hubConnection.StreamAsChannelAsync<int>("testMethod", cancellationToken: new CancellationToken(canceled: true)).DefaultTimeout());

            await hubConnection.StopAsync().DefaultTimeout();

            // Assert that StreamAsChannelAsync didn't send a message
            Assert.Equal("{\"type\":7}", await connection.ReadSentTextMessageAsync().DefaultTimeout());
        }
    }

    [Fact]
    public async Task StreamAsyncCanceledWhenPassedCanceledToken()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();
            var result = hubConnection.StreamAsync<int>("testMethod", cancellationToken: new CancellationToken(canceled: true));
            await Assert.ThrowsAsync<TaskCanceledException>(() => result.GetAsyncEnumerator().MoveNextAsync().DefaultTimeout());

            await hubConnection.StopAsync().DefaultTimeout();

            // Assert that StreamAsync didn't send a message
            Assert.Equal("{\"type\":7}", await connection.ReadSentTextMessageAsync().DefaultTimeout());
        }
    }

    [Fact]
    public async Task CanCancelTokenAfterStreamIsCompleted()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();

            var asyncEnumerable = hubConnection.StreamAsync<int>("Stream", 1);
            using var cts = new CancellationTokenSource();
            await using var e = asyncEnumerable.GetAsyncEnumerator(cts.Token);
            var task = e.MoveNextAsync();

            var item = await connection.ReadSentJsonAsync().DefaultTimeout();
            await connection.ReceiveJsonMessage(
                new { type = HubProtocolConstants.CompletionMessageType, invocationId = item["invocationId"] }
                ).DefaultTimeout();

            await task.DefaultTimeout();

            while (await e.MoveNextAsync().DefaultTimeout())
            {
            }
            // Cancel after stream is completed but before the AsyncEnumerator is disposed
            cts.Cancel();
        }
    }

    [Fact]
    public async Task CanCancelTokenDuringStream_SendsCancelInvocation()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);

            await hubConnection.StartAsync().DefaultTimeout();

            using var cts = new CancellationTokenSource();
            var asyncEnumerable = hubConnection.StreamAsync<int>("Stream", 1, cts.Token);

            await using var e = asyncEnumerable.GetAsyncEnumerator(cts.Token);
            var task = e.MoveNextAsync();

            var item = await connection.ReadSentJsonAsync().DefaultTimeout();
            var invocationId = item["invocationId"];
            await connection.ReceiveJsonMessage(
                new { type = HubProtocolConstants.StreamItemMessageType, invocationId, item = 1 }
                ).DefaultTimeout();

            await task.DefaultTimeout();
            cts.Cancel();

            item = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.CancelInvocationMessageType, item["type"]);
            Assert.Equal(invocationId, item["invocationId"]);

            // Stream on client-side completes on cancellation
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await e.MoveNextAsync()).DefaultTimeout();
        }
    }

    [Fact]
    public async Task ConnectionTerminatedIfServerTimeoutIntervalElapsesWithNoMessages()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   writeContext.EventId.Name == "ShutdownWithError";
        }
        using (StartVerifiableLog(ExpectedErrors))
        {
            var hubConnection = CreateHubConnection(new TestConnection(), loggerFactory: LoggerFactory);
            hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(100);

            var closeTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += ex =>
            {
                closeTcs.TrySetResult(ex);
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync().DefaultTimeout();

            var exception = Assert.IsType<TimeoutException>(await closeTcs.Task.DefaultTimeout());

            // We use an interpolated string so the tests are accurate on non-US machines.
            Assert.Equal($"Server timeout ({hubConnection.ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.", exception.Message);
        }
    }

    [Fact]
    public async Task ServerTimeoutIsDisabledWhenUsingTransportWithInherentKeepAlive()
    {
        using (StartVerifiableLog())
        {
            var testConnection = new TestConnection();
            testConnection.Features.Set<IConnectionInherentKeepAliveFeature>(new TestKeepAliveFeature() { HasInherentKeepAlive = true });
            var hubConnection = CreateHubConnection(testConnection, loggerFactory: LoggerFactory);
            hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(1);

            await hubConnection.StartAsync().DefaultTimeout();

            var closeTcs = new TaskCompletionSource<Exception>();
            hubConnection.Closed += ex =>
            {
                closeTcs.TrySetResult(ex);
                return Task.CompletedTask;
            };

            await hubConnection.RunTimerActions().DefaultTimeout();

            Assert.False(closeTcs.Task.IsCompleted);

            await hubConnection.DisposeAsync().DefaultTimeout();
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task PendingInvocationsAreTerminatedIfServerTimeoutIntervalElapsesWithNoMessages()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   writeContext.EventId.Name == "ShutdownWithError";
        }

        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var hubConnection = CreateHubConnection(new TestConnection(), loggerFactory: LoggerFactory);
            hubConnection.ServerTimeout = TimeSpan.FromMilliseconds(2000);

            await hubConnection.StartAsync().DefaultTimeout();

            // Start an invocation (but we won't complete it)
            var invokeTask = hubConnection.InvokeAsync("Method").DefaultTimeout();

            var exception = await Assert.ThrowsAsync<TimeoutException>(() => invokeTask);

            // We use an interpolated string so the tests are accurate on non-US machines.
            Assert.Equal($"Server timeout ({hubConnection.ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.", exception.Message);
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task StreamIntsToServer()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<int>("SomeMethod", channel.Reader);

            var invocation = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invocation["type"]);
            Assert.Equal("SomeMethod", invocation["target"]);
            var streamId = invocation["streamIds"][0];

            foreach (var number in new[] { 42, 43, 322, 3145, -1234 })
            {
                await channel.Writer.WriteAsync(number).AsTask().DefaultTimeout();

                var item = await connection.ReadSentJsonAsync().DefaultTimeout();
                Assert.Equal(HubProtocolConstants.StreamItemMessageType, item["type"]);
                Assert.Equal(number, item["item"]);
                Assert.Equal(streamId, item["invocationId"]);
            }

            channel.Writer.TryComplete();
            var completion = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.CompletionMessageType, completion["type"]);

            await connection.ReceiveJsonMessage(
                new { type = HubProtocolConstants.CompletionMessageType, invocationId = invocation["invocationId"], result = 42 }
                ).DefaultTimeout();
            var result = await invokeTask.DefaultTimeout();
            Assert.Equal(42, result);
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task StreamIntsToServerViaSend()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var channel = Channel.CreateUnbounded<int>();
            var sendTask = hubConnection.SendAsync("SomeMethod", channel.Reader);

            var invocation = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invocation["type"]);
            Assert.Equal("SomeMethod", invocation["target"]);
            Assert.Null(invocation["invocationId"]);
            var streamId = invocation["streamIds"][0];

            foreach (var item in new[] { 2, 3, 10, 5 })
            {
                await channel.Writer.WriteAsync(item);

                var received = await connection.ReadSentJsonAsync().DefaultTimeout();
                Assert.Equal(HubProtocolConstants.StreamItemMessageType, received["type"]);
                Assert.Equal(item, received["item"]);
                Assert.Equal(streamId, received["invocationId"]);
            }
        }
    }

    [Fact(Skip = "Objects not supported yet")]
    [LogLevel(LogLevel.Trace)]
    public async Task StreamsObjectsToServer()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var channel = Channel.CreateUnbounded<object>();
            var invokeTask = hubConnection.InvokeAsync<SampleObject>("UploadMethod", channel.Reader);

            var invocation = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invocation["type"]);
            Assert.Equal("UploadMethod", invocation["target"]);
            var id = invocation["invocationId"];

            var items = new[] { new SampleObject("ab", 12), new SampleObject("ef", 23) };
            foreach (var item in items)
            {
                await channel.Writer.WriteAsync(item);

                var received = await connection.ReadSentJsonAsync().DefaultTimeout();
                Assert.Equal(HubProtocolConstants.StreamItemMessageType, received["type"]);
                Assert.Equal(item.Foo, received["item"]["foo"]);
                Assert.Equal(item.Bar, received["item"]["bar"]);
            }

            channel.Writer.TryComplete();
            var completion = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.CompletionMessageType, completion["type"]);

            var expected = new SampleObject("oof", 14);
            await connection.ReceiveJsonMessage(
                new { type = HubProtocolConstants.CompletionMessageType, invocationId = id, result = expected }
                ).DefaultTimeout();
            var result = await invokeTask.DefaultTimeout();

            Assert.Equal(expected.Foo, result.Foo);
            Assert.Equal(expected.Bar, result.Bar);
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task UploadStreamCancellationSendsStreamComplete()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<object>("UploadMethod", channel.Reader, cts.Token);

            var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invokeMessage["type"]);

            cts.Cancel();

            // after cancellation, don't send from the pipe
            foreach (var number in new[] { 42, 43, 322, 3145, -1234 })
            {
                await channel.Writer.WriteAsync(number);
            }

            // the next sent message should be a completion message
            var complete = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.CompletionMessageType, complete["type"]);
            Assert.EndsWith("canceled by client.", ((string)complete["error"]));
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task UploadStreamErrorSendsStreamComplete()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var cts = new CancellationTokenSource();
            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<object>("UploadMethod", channel.Reader, cts.Token);

            var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invokeMessage["type"]);

            channel.Writer.Complete(new Exception("error from client"));

            // the next sent message should be a completion message
            var complete = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.CompletionMessageType, complete["type"]);
            Assert.StartsWith("Stream errored by client: 'System.Exception: error from client", ((string)complete["error"]));
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task ActiveUploadStreamWhenConnectionClosesObservesException()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<object>("UploadMethod", channel.Reader);

            var invokeMessage = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invokeMessage["type"]);

            // Not sure how to test for unobserved task exceptions, best I could come up with is to check that we log where there once was an unobserved task exception
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TestSink.MessageLogged += wc =>
            {
                if (wc.EventId.Name == "CompletingStreamNotSent")
                {
                    tcs.SetResult();
                }
            };

            await hubConnection.StopAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() => invokeTask).DefaultTimeout();

            await tcs.Task.DefaultTimeout();
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task InvocationCanCompleteBeforeStreamCompletes()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<long>("UploadMethod", channel.Reader);
            var invocation = await connection.ReadSentJsonAsync().DefaultTimeout();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invocation["type"]);
            var id = invocation["invocationId"];

            await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.CompletionMessageType, invocationId = id, result = 10 });

            var result = await invokeTask.DefaultTimeout();
            Assert.Equal(10L, result);

            // after the server returns, with whatever response
            // the client's behavior is undefined, and the server is responsible for ignoring stray messages
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task WrongTypeOnServerResponse()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == typeof(HubConnection).FullName &&
                   (writeContext.EventId.Name == "ServerDisconnectedWithError"
                    || writeContext.EventId.Name == "ShutdownWithError");
        }
        using (StartVerifiableLog(ExpectedErrors))
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            // we expect to get sent ints, and receive an int back
            var channel = Channel.CreateUnbounded<int>();
            var invokeTask = hubConnection.InvokeAsync<int>("SumInts", channel.Reader);

            var invocation = await connection.ReadSentJsonAsync();
            Assert.Equal(HubProtocolConstants.InvocationMessageType, invocation["type"]);
            var id = invocation["invocationId"];

            await channel.Writer.WriteAsync(5);
            await channel.Writer.WriteAsync(10);

            await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.CompletionMessageType, invocationId = id, result = "humbug" });

            try
            {
                await invokeTask;
                Assert.True(false);
            }
            catch (Exception)
            {
            }
        }
    }

    [Fact]
    public async Task CanAwaitInvokeFromOnHandlerWithServerClosingConnection()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            hubConnection.On<string>("Echo", async msg =>
            {
                try
                {
                    // This should be canceled when the connection is closed
                    await hubConnection.InvokeAsync<string>("Echo", msg).DefaultTimeout();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }

                tcs.SetResult();
            });

            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            hubConnection.Closed += _ =>
            {
                closedTcs.SetResult();

                return Task.CompletedTask;
            };

            await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.InvocationMessageType, target = "Echo", arguments = new object[] { "42" } }).DefaultTimeout();

            // Read sent message first to make sure invoke has been processed and is waiting for a response
            await connection.ReadSentJsonAsync().DefaultTimeout();
            await connection.ReceiveJsonMessage(new { type = HubProtocolConstants.CloseMessageType }).DefaultTimeout();

            await closedTcs.Task.DefaultTimeout();

            await Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task.DefaultTimeout());
        }
    }

    [Fact]
    public async Task CanAwaitUsingHubConnection()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            await using var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();
        }
    }

    [Fact]
    public async Task VerifyUserOptionsAreNotChanged()
    {
        using (StartVerifiableLog())
        {
            HttpConnectionOptions originalOptions = null, resolvedOptions = null;
            var accessTokenFactory = new Func<Task<string>>(() => Task.FromResult("fakeAccessToken"));
            var fakeHeader = "fakeHeader";

            var connection = new HubConnectionBuilder()
                .WithUrl("http://example.com", Http.Connections.HttpTransportType.WebSockets,
                    options =>
                    {
                        originalOptions = options;
                        options.SkipNegotiation = true;
                        options.Headers.Add(fakeHeader, "value");
                        options.AccessTokenProvider = accessTokenFactory;
                        options.WebSocketFactory = (context, token) =>
                        {
                            resolvedOptions = context.Options;
                            return ValueTask.FromResult<WebSocket>(null);
                        };
                    })
                .Build();

            try
            {
                // since we returned null WebSocket it would fail
                await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync().DefaultTimeout());
            }
            finally
            {
                await connection.DisposeAsync().DefaultTimeout();
            }

            Assert.NotNull(resolvedOptions);
            Assert.NotNull(originalOptions);
            // verify that object was copied
            Assert.NotSame(resolvedOptions, originalOptions);
            Assert.NotSame(resolvedOptions.AccessTokenProvider, originalOptions.AccessTokenProvider);
            // verify original object still points to the same provider
            Assert.Same(originalOptions.AccessTokenProvider, accessTokenFactory);
            Assert.Same(resolvedOptions.Headers, originalOptions.Headers);
            Assert.Contains(fakeHeader, resolvedOptions.Headers);
        }
    }

    [Fact]
    [LogLevel(LogLevel.Trace)]
    public async Task ClientResultResponseAfterConnectionCloseObservesException()
    {
        using (StartVerifiableLog())
        {
            var connection = new TestConnection();
            var hubConnection = CreateHubConnection(connection, loggerFactory: LoggerFactory);
            await hubConnection.StartAsync().DefaultTimeout();

            var resultTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            hubConnection.On("Result", async () =>
            {
                await resultTcs.Task;
                return 1;
            });

            await connection.ReceiveTextAsync("{\"type\":1,\"invocationId\":\"1\",\"target\":\"Result\",\"arguments\":[]}\u001e").DefaultTimeout();

            // Not sure how to test for unobserved task exceptions, best I could come up with is to check that we log where there once was an unobserved task exception
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TestSink.MessageLogged += wc =>
            {
                if (wc.EventId.Name == "ErrorSendingInvocationResult")
                {
                    tcs.SetResult();
                }
            };

            await hubConnection.StopAsync();
            resultTcs.SetResult();

            await tcs.Task.DefaultTimeout();
        }
    }

    [Fact]
    public async Task HubConnectionIsMockable()
    {
        var mockConnection = new Mock<HubConnection>(new Mock<IConnectionFactory>().Object, new Mock<IHubProtocol>().Object, new Mock<EndPoint>().Object,
            new Mock<IServiceProvider>().Object, new Mock<ILoggerFactory>().Object, new Mock<IRetryPolicy>().Object);

        mockConnection.Setup(c => c.StartAsync(default)).Returns(() => Task.CompletedTask);
        mockConnection.Setup(c => c.StopAsync(default)).Returns(() => Task.CompletedTask);
        mockConnection.Setup(c => c.DisposeAsync()).Returns(() => ValueTask.CompletedTask);
        mockConnection.Setup(c => c.On(It.IsAny<string>(), It.IsAny<Type[]>(), It.IsAny<Func<object[], object, Task>>(), It.IsAny<object>()));
        mockConnection.Setup(c => c.Remove(It.IsAny<string>()));
        mockConnection.Setup(c => c.StreamAsChannelCoreAsync(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(It.IsAny<ChannelReader<object>>()));
        mockConnection.Setup(c => c.InvokeCoreAsync(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(It.IsAny<object>()));
        mockConnection.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(() => Task.CompletedTask);
        mockConnection.Setup(c => c.StreamAsyncCore<object>(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(() => It.IsAny<IAsyncEnumerable<object>>());

        var hubConnection = mockConnection.Object;
        // .On extension method
        _ = hubConnection.On("someMethod", () => { });
        // .On non-extension method
        _ = hubConnection.On("someMethod2", new Type[] { typeof(int) }, (args, obj) => Task.CompletedTask, 2);
        hubConnection.Remove("someMethod");
        await hubConnection.StartAsync();
        _ = await hubConnection.StreamAsChannelCoreAsync("stream", typeof(int), Array.Empty<object>(), default);
        _ = await hubConnection.InvokeCoreAsync("test", typeof(int), Array.Empty<object>(), default);
        await hubConnection.SendCoreAsync("test2", Array.Empty<object>(), default);
        _ = hubConnection.StreamAsyncCore<int>("stream2", Array.Empty<object>(), default);
        await hubConnection.StopAsync();

        mockConnection.Verify(c => c.On("someMethod", It.IsAny<Type[]>(), It.IsAny<Func<object[], object, Task>>(), It.IsAny<object>()), Times.Once);
        mockConnection.Verify(c => c.On("someMethod2", It.IsAny<Type[]>(), It.IsAny<Func<object[], object, Task>>(), 2), Times.Once);
        mockConnection.Verify(c => c.Remove("someMethod"), Times.Once);
        mockConnection.Verify(c => c.StreamAsChannelCoreAsync("stream", It.IsAny<Type>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        mockConnection.Verify(c => c.InvokeCoreAsync("test", typeof(int), Array.Empty<object>(), It.IsAny<CancellationToken>()), Times.Once);
        mockConnection.Verify(c => c.SendCoreAsync("test2", Array.Empty<object>(), It.IsAny<CancellationToken>()), Times.Once);
        mockConnection.Verify(c => c.StreamAsyncCore<int>("stream2", Array.Empty<object>(), It.IsAny<CancellationToken>()), Times.Once);
        mockConnection.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockConnection.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableReconnectCalledWhenCloseMessageReceived()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://example.com");
        var innerConnection = new TestConnection();
        var reconnectFeature = new TestReconnectFeature();
#pragma warning disable CA2252 // This API requires opting into preview features
        innerConnection.Features.Set<IStatefulReconnectFeature>(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features

        var delegateConnectionFactory = new DelegateConnectionFactory(
            endPoint => innerConnection.StartAsync());
        builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

        var hubConnection = builder.Build();
        var closedEventTcs = new TaskCompletionSource<Exception>();
        hubConnection.Closed += e =>
        {
            closedEventTcs.SetResult(e);
            return Task.CompletedTask;
        };

        await hubConnection.StartAsync().DefaultTimeout();

        await innerConnection.ReceiveJsonMessage(new { type = HubProtocolConstants.CloseMessageType });

        var exception = await closedEventTcs.Task.DefaultTimeout();
        Assert.Null(exception);

        await reconnectFeature.DisableReconnectCalled.DefaultTimeout();
    }

    [Fact]
    public async Task DisableReconnectCalledWhenSendingCloseMessage()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://example.com");
        var innerConnection = new TestConnection();
        var reconnectFeature = new TestReconnectFeature();
#pragma warning disable CA2252 // This API requires opting into preview features
        innerConnection.Features.Set<IStatefulReconnectFeature>(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features

        var delegateConnectionFactory = new DelegateConnectionFactory(
            endPoint => innerConnection.StartAsync());
        builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

        var hubConnection = builder.Build();
        var closedEventTcs = new TaskCompletionSource<Exception>();
        hubConnection.Closed += e =>
        {
            closedEventTcs.SetResult(e);
            return Task.CompletedTask;
        };

        await hubConnection.StartAsync().DefaultTimeout();

        await hubConnection.StopAsync().DefaultTimeout();

        var exception = await closedEventTcs.Task.DefaultTimeout();
        Assert.Null(exception);

        await reconnectFeature.DisableReconnectCalled.DefaultTimeout();
    }

    private class SampleObject
    {
        public SampleObject(string foo, int bar)
        {
            Foo = foo;
            Bar = bar;
        }

        public string Foo { get; private set; }
        public int Bar { get; private set; }
    }

    private struct TestKeepAliveFeature : IConnectionInherentKeepAliveFeature
    {
        public bool HasInherentKeepAlive { get; set; }
    }

    // Moq really doesn't handle out parameters well, so to make these tests work I added a manual mock -anurse
    private class MockHubProtocol : IHubProtocol
    {
        private HubInvocationMessage _parsed;
        private Exception _error;

        public static MockHubProtocol ReturnOnParse(HubInvocationMessage parsed)
        {
            return new MockHubProtocol
            {
                _parsed = parsed
            };
        }

        public static MockHubProtocol Throw(Exception error)
        {
            return new MockHubProtocol
            {
                _error = error
            };
        }

        public string Name => "MockHubProtocol";
        public int Version => 1;
        public int MinorVersion => 1;

        public TransferFormat TransferFormat => TransferFormat.Binary;

        public bool IsVersionSupported(int version)
        {
            return true;
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (_error != null)
            {
                throw _error;
            }
            if (_parsed != null)
            {
                message = _parsed;
                return true;
            }

            throw new InvalidOperationException("No Parsed Message provided");
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            if (message is PingMessage)
            {
                // Allows HubConnection.StartAsync() to complete successfully
                // when testing InvokeThrowsIfSerializingMessageFails and SendAsyncThrowsIfSerializingMessageFails
                return;
            }
            if (_error != null)
            {
                throw _error;
            }
        }

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }
    }

#pragma warning disable CA2252 // This API requires opting into preview features
    private sealed class TestReconnectFeature : IStatefulReconnectFeature
#pragma warning restore CA2252 // This API requires opting into preview features
    {
        private TaskCompletionSource _disableReconnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task DisableReconnectCalled => _disableReconnect.Task;

#pragma warning disable CA2252 // This API requires opting into preview features
        public void OnReconnected(Func<PipeWriter, Task> notifyOnReconnected) { }
#pragma warning restore CA2252 // This API requires opting into preview features

#pragma warning disable CA2252 // This API requires opting into preview features
        public void DisableReconnect()
#pragma warning restore CA2252 // This API requires opting into preview features
        {
            _disableReconnect.TrySetResult();
        }
    }
}
