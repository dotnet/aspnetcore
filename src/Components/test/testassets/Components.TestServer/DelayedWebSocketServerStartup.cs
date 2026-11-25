// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Connections;

namespace TestServer;

public sealed class DelayedWebSocketServerStartup(IConfiguration configuration)
{
    private TaskCompletionSource? _incomingMessagesGate;

    public void ConfigureServices(IServiceCollection services)
    {
        new ServerStartup(configuration).ConfigureServices(services);
        services.AddSingleton(this);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(ResumeIncomingMessages);
        app.Map("/subdir", app =>
        {
            app.UseStaticFiles();
            app.UseRouting();
            app.UseWebSockets();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub(options => options.Transports = HttpTransportType.WebSockets)
                    .AddEndpointFilter(async (context, next) =>
                    {
                        var feature = context.HttpContext.Features.Get<IHttpWebSocketFeature>()!;
                        context.HttpContext.Features.Set<IHttpWebSocketFeature>(new DelayedSocketFeature(feature, this));
                        return await next(context);
                    });
                endpoints.MapFallbackToPage("/_ServerHost");
            });
        });
    }

    public void PauseIncomingMessages()
    {
        Volatile.Write(ref _incomingMessagesGate, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public void ResumeIncomingMessages()
    {
        Interlocked.Exchange(ref _incomingMessagesGate, null)?.TrySetResult();
    }

    private sealed class DelayedSocketFeature(IHttpWebSocketFeature original, DelayedWebSocketServerStartup startup) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => original.IsWebSocketRequest;

        public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            context.DangerousEnableCompression = true;
            return new DelayedSocket(await original.AcceptAsync(context), startup);
        }
    }

    private sealed class DelayedSocket(WebSocket inner, DelayedWebSocketServerStartup startup) : WebSocket
    {
        public override WebSocketCloseStatus? CloseStatus => inner.CloseStatus;
        public override string? CloseStatusDescription => inner.CloseStatusDescription;
        public override WebSocketState State => inner.State;
        public override string? SubProtocol => inner.SubProtocol;

        public override void Abort() => inner.Abort();
        public override void Dispose() => inner.Dispose();
        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => inner.CloseAsync(closeStatus, statusDescription, cancellationToken);
        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => inner.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            => inner.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var result = await inner.ReceiveAsync(buffer, cancellationToken);
            var gate = Volatile.Read(ref startup._incomingMessagesGate);
            if (result.MessageType is not WebSocketMessageType.Close && gate is not null)
            {
                // Delay delivery after receiving so an already-pending read cannot bypass the gate.
                await gate.Task.WaitAsync(cancellationToken);
            }

            return result;
        }
    }
}
