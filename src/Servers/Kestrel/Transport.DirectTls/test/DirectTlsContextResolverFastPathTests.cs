// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Covers the complement of <c>DirectTlsUserCallbackDispatchTests</c>: when resolving the TLS context provably
/// cannot reach user code - no endpoint certificate selector and no ClientHello listener - the pump must resolve
/// inline instead of suspending the handshake onto the thread pool. The bootstrap context carries no credentials,
/// so every connection still reaches <c>NeedsTlsContext</c>; the question these tests answer is only which thread
/// the resolver runs on. Pump threads are named <c>TlsEventPump-{id}</c>, which makes that directly observable.
/// </summary>
public class DirectTlsContextResolverFastPathTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private const string PumpThreadNamePrefix = "TlsEventPump-";

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ContextResolver_WithoutSelectorOrClientHelloListener_RunsOnThePumpThread()
    {
        // The fast path: a static certificate behind the transport's own resolver. Nothing here can block for an
        // unbounded time, so suspending would buy nothing and cost a thread-pool round trip per connection.
        var threadName = await CaptureResolverThreadNameAsync(serverCertificateSelectorConfigured: false);

        Assert.StartsWith(PumpThreadNamePrefix, threadName);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ContextResolver_WithCertificateSelector_RunsOffThePumpThread()
    {
        // The endpoint supplied a selector, so the resolver closes over user code and must be suspended.
        var threadName = await CaptureResolverThreadNameAsync(serverCertificateSelectorConfigured: true);

        Assert.DoesNotContain(PumpThreadNamePrefix, threadName ?? string.Empty);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ContextResolver_WithClientHelloListenerOnly_RunsOffThePumpThread()
    {
        // No selector, but a ClientHello listener - which the pump runs immediately before resolving the context.
        // The pump must combine that with the selector flag rather than trusting the flag alone, otherwise a
        // listener-only endpoint would silently run user code on the event loop.
        var threadName = await CaptureResolverThreadNameAsync(
            serverCertificateSelectorConfigured: false,
            withClientHelloListener: true);

        Assert.DoesNotContain(PumpThreadNamePrefix, threadName ?? string.Empty);
    }

    // Drives one real TLS handshake against a listener whose context resolver records the thread it was invoked
    // on, and returns that thread's name. Only the resolver call is awaited: whether the handshake goes on to
    // succeed is irrelevant to which thread resolved the context.
    private static async Task<string?> CaptureResolverThreadNameAsync(
        bool serverCertificateSelectorConfigured,
        bool withClientHelloListener = false)
    {
        using var certificate = TestResources.GetTestCertificate();
        using var bootstrapContext = TlsContext.CreateServer(new SslServerAuthenticationOptions());
        using var serverContext = TlsContext.CreateServer(new SslServerAuthenticationOptions
        {
            ServerCertificate = certificate,
        });

        var resolverThreadName = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var listener = new DirectTlsConnectionListener(
            NullLoggerFactory.Instance,
            bootstrapContext,
            (connection, hostName) =>
            {
                resolverThreadName.TrySetResult(Thread.CurrentThread.Name);
                return (serverContext, null);
            },
            new TlsEventPumpPool(pumpCount: 1, NullLoggerFactory.Instance),
            new IPEndPoint(IPAddress.Loopback, 0),
            new DirectTlsTransportOptions(),
            MemoryPool<byte>.Shared,
            new TestHostApplicationLifetime(),
            withClientHelloListener ? static (_, _) => { } : null,
            ownedServerContexts: null,
            serverCertificateSelectorConfigured);

        listener.Bind();

        // Keep the ready-connection channel drained so a completed handshake does not park a connection for the
        // lifetime of the test. Ends on its own when DisposeAsync completes the channel.
        var drain = Task.Run(async () =>
        {
            while (await listener.AcceptAsync() is { } connection)
            {
                await connection.DisposeAsync();
            }
        });

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync((IPEndPoint)listener.EndPoint);

            using var sslStream = new SslStream(client.GetStream(), leaveInnerStreamOpen: false, (_, _, _, _) => true);
            var handshake = sslStream.AuthenticateAsClientAsync("localhost");

            var threadName = await resolverThreadName.Task.WaitAsync(Timeout);

            // The resolver ran off the pump, but the handshake itself must still succeed: these tests use a
            // real certificate and a permissive client callback, so a failure here means something broke.
            await handshake.WaitAsync(Timeout);

            return threadName;
        }
        finally
        {
            await listener.DisposeAsync();

            // DisposeAsync completes the accept channel, so the drain loop ends on its own. Awaiting it keeps
            // the loop from outliving the test and surfaces any failure it hit.
            await drain.WaitAsync(Timeout);
        }
    }
}
