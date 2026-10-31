// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public partial class HubConnectionTests
{
    [Theory]
    [InlineData(HttpTransportType.WebSockets, false, "json")]
    [InlineData(HttpTransportType.WebSockets, true, "json")]
    [InlineData(HttpTransportType.ServerSentEvents, false, "json")]
    [InlineData(HttpTransportType.ServerSentEvents, true, "json")]
    [InlineData(HttpTransportType.LongPolling, false, "json")]
    [InlineData(HttpTransportType.LongPolling, true, "json")]
    [InlineData(HttpTransportType.WebSockets, false, "messagepack")]
    [InlineData(HttpTransportType.WebSockets, true, "messagepack")]
    [InlineData(HttpTransportType.LongPolling, false, "messagepack")]
    [InlineData(HttpTransportType.LongPolling, true, "messagepack")]
    public async Task MessageContentLoggingPreservesInvocations(HttpTransportType transport, bool enabled, string protocolName)
    {
        await using var server = await StartServer<Startup>();
        var sink = new TestSink();
        using var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var builder = new HubConnectionBuilder()
            .WithLoggerFactory(loggerFactory)
            .WithUrl(server.Url + "/default", options =>
            {
                options.Transports = transport;
                options.LogMessageContent = enabled;
                options.HttpMessageHandlerFactory = handler =>
                {
                    ((HttpClientHandler)handler).UseProxy = false;
                    return handler;
                };
                options.WebSocketConfiguration = socket => socket.Proxy = new WebProxy();
            });
        builder.Services.AddSingleton<IHubProtocol>(HubProtocols[protocolName]);
        var connection = builder.Build();
        const string marker = "CONTENT_LOG_MARKER";
        var message = marker + new string('a', 1500) + "END_OF_MESSAGE";
        try
        {
            await connection.StartAsync().DefaultTimeout();
            Assert.Equal(message, await connection.InvokeAsync<string>(nameof(TestHub.Echo), message).DefaultTimeout());
            var writes = sink.Writes.Where(write => write.EventId.Name is "ReceivedMessageContent" or "SendingMessageContent").ToArray();
            if (enabled)
            {
                Assert.Contains(writes, write => write.EventId.Name == "SendingMessageContent" && write.Message.Contains(marker));
                Assert.Contains(writes, write => write.EventId.Name == "ReceivedMessageContent" && write.Message.Contains(marker));
                Assert.Contains(writes, write => Fields(write)["Truncated"] is true);
                Assert.All(writes, write => Assert.InRange((int)Fields(write)["LoggedByteCount"], 0, 1024));
            }
            else
            {
                Assert.Empty(writes);
            }
        }
        finally
        {
            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    private static Dictionary<string, object> Fields(WriteContext write) =>
        ((IEnumerable<KeyValuePair<string, object>>)write.State).ToDictionary(pair => pair.Key, pair => pair.Value);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MessageContentIsAvailableWhenHubProtocolParsingFails(bool enabled)
    {
        await using var server = await StartServer<MalformedMessageStartup>();
        var sink = new TestSink();
        using var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var closed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new HubConnectionBuilder()
            .WithLoggerFactory(loggerFactory)
            .WithUrl(server.Url, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.LogMessageContent = enabled;
                options.WebSocketConfiguration = socket => socket.Proxy = new WebProxy();
            }).Build();
        connection.Closed += error =>
        {
            closed.TrySetResult(error);
            return Task.CompletedTask;
        };
        try
        {
            await connection.StartAsync().DefaultTimeout();
            Assert.Equal(HubConnectionState.Connected, connection.State);
            server.Services.GetRequiredService<TaskCompletionSource>().SetResult();
            var error = await closed.Task.DefaultTimeout();
            Assert.IsType<InvalidDataException>(error);
            Assert.Equal("Error reading JSON.", error.Message);
            var writes = sink.Writes.Where(write => write.EventId.Name == "ReceivedMessageContent").ToArray();
            Assert.Equal(enabled, writes.Any(write => write.Message.Contains("MALFORMED_CONTENT_MARKER")));
        }
        finally
        {
            await connection.DisposeAsync().DefaultTimeout();
        }
    }

    public class MalformedMessageStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        public void Configure(IApplicationBuilder app, TaskCompletionSource sendMalformedMessage)
        {
            app.UseWebSockets();
            app.Run(async context =>
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                cancellation.CancelAfter(TimeSpan.FromSeconds(10));
                var buffer = new byte[1024];
                await ReceiveMessage(socket, buffer, cancellation.Token);
                await socket.SendAsync(Encoding.UTF8.GetBytes("{}\u001e"), WebSocketMessageType.Text, true, cancellation.Token);
                // StartAsync sends an initial ping. Only the test, after startup completes,
                // may trigger malformed data; receiving a ping must not trigger it.
                await sendMalformedMessage.Task.WaitAsync(cancellation.Token);
                var payload = Encoding.UTF8.GetBytes("{\"type\":1,\"target\":\"Bad\",\"arguments\":[\"MALFORMED_CONTENT_MARKER\"{\u001e");
                await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellation.Token);
                try
                {
                    while ((await socket.ReceiveAsync(buffer, cancellation.Token)).MessageType != WebSocketMessageType.Close)
                    {
                        // Ignore queued pings while the client rejects the malformed data.
                    }
                }
                catch (WebSocketException)
                {
                    // The client may abort after its protocol parser rejects the data.
                }
            });
        }

        private static async Task ReceiveMessage(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
        {
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.EndOfMessage)
                {
                    return;
                }
            }
        }
    }
}
