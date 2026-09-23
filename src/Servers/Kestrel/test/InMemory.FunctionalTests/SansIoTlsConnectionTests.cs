// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

/// <summary>
/// Covers the sans-IO TLS layer directly, rather than relying on the whole suite being re-run
/// with the switch set at build time - which CI does not do, so without these the path has no
/// coverage at all.
///
/// <para>The switch is read per connection, so it can be flipped for the lifetime of one test
/// class. Tests that must observe the SslStream path assert it explicitly instead of assuming
/// which layer ran: a silent fallback would otherwise make these tests pass while measuring
/// nothing, which is the same trap the benchmarks had to guard against.</para>
/// </summary>
/// <summary>
/// The sans-IO layer is enabled by a process-wide AppContext switch, so these tests cannot
/// run alongside anything else that opens a TLS connection - a concurrent test would silently
/// get the other TLS layer and assert against it. Disabling parallelisation for this one
/// collection keeps that contained without serialising the whole assembly.
/// </summary>
[CollectionDefinition(SansIoTlsCollection.Name, DisableParallelization = true)]
public sealed class SansIoTlsCollection
{
    public const string Name = "SansIoTls";
}

[Collection(SansIoTlsCollection.Name)]
public class SansIoTlsConnectionTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    private readonly bool _switchWasSet;
    private readonly bool _previousValue;

    public SansIoTlsConnectionTests()
    {
        _switchWasSet = AppContext.TryGetSwitch(SansIoTlsSupport.EnableSwitch, out _previousValue);
        AppContext.SetSwitch(SansIoTlsSupport.EnableSwitch, true);
    }

    public override void Dispose()
    {
        AppContext.SetSwitch(SansIoTlsSupport.EnableSwitch, _switchWasSet && _previousValue);
        base.Dispose();
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task ServesRequestsAndPublishesNegotiatedTlsValues()
    {
        SslProtocols? protocol = null;
        object cipherSuite = null;
        var hasSslStreamFeature = true;

        await using var server = new TestServer(
            context =>
            {
                var handshake = context.Features.Get<ITlsHandshakeFeature>();
                protocol = handshake?.Protocol;
                cipherSuite = handshake?.NegotiatedCipherSuite;
                hasSslStreamFeature = context.Features.Get<ISslStreamFeature>() is not null;
                return WriteBody(context, "hello world");
            },
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(_x509Certificate2));

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync("localhost");
        await AssertResponse(stream, "hello world");

        Assert.NotNull(protocol);
        Assert.NotEqual(SslProtocols.None, protocol.Value);
        Assert.NotNull(cipherSuite);

        // Deliberate and documented: there is no SslStream on this path, so the feature is
        // absent rather than present-but-throwing.
        Assert.False(hasSslStreamFeature);
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task RoundTripsBodyLargerThanASingleTlsRecord()
    {
        // Larger than the 16 KB record limit, so the reader crosses several records and
        // several AdvanceTo cycles, and the writer emits several records.
        var body = new string('a', 128 * 1024);

        await using var server = new TestServer(
            async context =>
            {
                using var reader = new StreamReader(context.Request.Body);
                var received = await reader.ReadToEndAsync();
                await WriteBody(context, received.Length.ToString(CultureInfo.InvariantCulture));
            },
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(_x509Certificate2));

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync("localhost");

        var request = Encoding.UTF8.GetBytes(
            $"POST / HTTP/1.0\r\nHost: localhost\r\nContent-Length: {body.Length}\r\n\r\n{body}");
        await stream.WriteAsync(request).DefaultTimeout();

        using var reader = new StreamReader(stream, Encoding.ASCII);
        var response = await reader.ReadToEndAsync().DefaultTimeout();
        Assert.StartsWith("HTTP/1.1 200 OK", response, StringComparison.Ordinal);
        Assert.EndsWith(body.Length.ToString(CultureInfo.InvariantCulture), response, StringComparison.Ordinal);
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task PeerClosingMidRecordFailsPromptlyRatherThanHanging()
    {
        // Regression: a peer that sends a partial record - or an alert - and then disconnects
        // used to leave the handshake loop spinning. ReadAsync kept returning the same
        // completed buffer, the session could not consume it, and nothing broke the loop, so
        // the connection burned a core until the handshake timeout and was then reported as a
        // timeout rather than a failure. This must complete far inside that timeout.
        await using var server = new TestServer(
            context => WriteBody(context, "unreachable"),
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(_x509Certificate2));

        using var connection = server.CreateConnection();

        // A truncated TLS record header: a handshake content type and a length the peer never
        // satisfies, then disconnect.
        await connection.Stream.WriteAsync(new byte[] { 0x16, 0x03, 0x01, 0x00, 0x40 }).DefaultTimeout();
        await connection.Stream.FlushAsync().DefaultTimeout();
        connection.ShutdownSend();

        // The server should observe the close and give up. Reading returns EOF promptly; the
        // bug made this wait for the 10 second handshake timeout instead.
        var buffer = new byte[16];
        var read = await connection.Stream.ReadAsync(buffer).DefaultTimeout();
        Assert.Equal(0, read);
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task RejectedClientCertificateFailsTheConnection()
    {
        // Regression: recording a rejected validation result does not fail the call that
        // records it, so the handshake loop used to exit on IsHandshakeComplete before the
        // rejection surfaced. The connection was then reported as established and handed to
        // the application.
        await using var server = new TestServer(
            context => WriteBody(context, "should not be reached"),
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                ClientCertificateValidation = (_, _, _) => false,
            }));

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await stream.AuthenticateAsClientAsync("localhost").DefaultTimeout();

            // Some platforms only surface the rejection on first use rather than during the
            // handshake itself; either is a valid rejection, but serving a request is not.
            var request = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
            await stream.WriteAsync(request).DefaultTimeout();
            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer).DefaultTimeout();
            if (read == 0)
            {
                throw new IOException("Connection closed by the server.");
            }
        }).DefaultTimeout();
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task PerConnectionOnAuthenticateStaysOnSslStream()
    {
        // OnAuthenticate is documented to run per connection, but this path resolves one
        // TlsContext per endpoint, so the configuration is excluded and must fall back. If it
        // ever stops falling back, the first connection's options would silently apply to
        // every later connection on the endpoint.
        var onAuthenticateCalls = 0;
        var hasSslStreamFeature = false;

        await using var server = new TestServer(
            context =>
            {
                hasSslStreamFeature = context.Features.Get<ISslStreamFeature>() is not null;
                return WriteBody(context, "hello world");
            },
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                OnAuthenticate = (_, _) => Interlocked.Increment(ref onAuthenticateCalls),
            }));

        for (var i = 0; i < 2; i++)
        {
            using var connection = server.CreateConnection();
            var stream = OpenSslStream(connection.Stream);
            await stream.AuthenticateAsClientAsync("localhost");
            await AssertResponse(stream, "hello world");
        }

        Assert.True(hasSslStreamFeature, "OnAuthenticate must keep the connection on the SslStream path.");
        Assert.Equal(2, onAuthenticateCalls);
    }

    [ConditionalFact]
    [SansIoTlsSupported]
    public async Task SwitchOffKeepsConnectionsOnSslStream()
    {
        AppContext.SetSwitch(SansIoTlsSupport.EnableSwitch, false);

        var hasSslStreamFeature = false;

        await using var server = new TestServer(
            context =>
            {
                hasSslStreamFeature = context.Features.Get<ISslStreamFeature>() is not null;
                return WriteBody(context, "hello world");
            },
            new TestServiceContext(LoggerFactory),
            listenOptions => listenOptions.UseHttps(_x509Certificate2));

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync("localhost");
        await AssertResponse(stream, "hello world");

        Assert.True(hasSslStreamFeature, "With the switch off the connection must use SslStream.");
    }

    // Without an explicit Content-Length Kestrel falls back to chunked encoding, and the
    // assertion below would read the chunk size rather than the body.
    private static Task WriteBody(HttpContext context, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Response.ContentLength = bytes.Length;
        return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
    }

    private static SslStream OpenSslStream(Stream rawStream)
        => new SslStream(rawStream, leaveInnerStreamOpen: false, (_, _, _, _) => true);

    // HTTP/1.0 so the server closes the connection after responding; with keep-alive the
    // reader would block waiting for a request that never comes.
    private static async Task AssertResponse(SslStream stream, string expectedBody)
    {
        var request = Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\nHost: localhost\r\n\r\n");
        await stream.WriteAsync(request).DefaultTimeout();

        using var reader = new StreamReader(stream, Encoding.ASCII);
        var response = await reader.ReadToEndAsync().DefaultTimeout();

        Assert.StartsWith("HTTP/1.1 200 OK", response, StringComparison.Ordinal);
        Assert.EndsWith(expectedBody, response, StringComparison.Ordinal);
    }
}
