// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Interop.FunctionalTests;

internal static class HttpHelpers
{
    public static HttpProtocolException GetProtocolException(this Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current is HttpProtocolException httpProtocolException)
            {
                return httpProtocolException;
            }

            current = current.InnerException;
        }

        throw new Exception($"Couldn't find {nameof(HttpProtocolException)}. Original error: {ex}");
    }

    public static HttpMessageInvoker CreateClient(TimeSpan? idleTimeout = null, TimeSpan? expect100ContinueTimeout = null, bool includeClientCert = false, int? maxResponseHeadersLength = null)
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, __, ___, ____) => true,
            TargetHost = "targethost",
            ClientCertificates = !includeClientCert ? null : new X509CertificateCollection() { TestResources.GetTestCertificate() },
        };

        if (expect100ContinueTimeout != null)
        {
            handler.Expect100ContinueTimeout = expect100ContinueTimeout.Value;
        }

        if (idleTimeout != null)
        {
            handler.PooledConnectionIdleTimeout = idleTimeout.Value;
        }

        if (maxResponseHeadersLength != null)
        {
            handler.MaxResponseHeadersLength = maxResponseHeadersLength.Value;
        }

        return new HttpMessageInvoker(handler);
    }

    public static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configureServices, RequestDelegate requestDelegate, HttpProtocols? protocol = null, Action<KestrelServerOptions> configureKestrel = null, bool? plaintext = null, TimeSpan? shutdownTimeout = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        if (configureKestrel == null)
                        {
                            o.Listen(IPAddress.Parse("127.0.0.1"), 0, listenOptions =>
                            {
                                listenOptions.Protocols = protocol ?? HttpProtocols.Http3;
                                if (!(plaintext ?? false))
                                {
                                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                                }
                            });
                        }
                        else
                        {
                            configureKestrel(o);
                        }
                    })
                    .Configure(app =>
                    {
                        app.Run(requestDelegate);
                    });
            })
            .ConfigureServices(configureServices)
            .ConfigureHostOptions(o =>
            {
                if (Debugger.IsAttached)
                {
                    // Avoid timeout while debugging.
                    o.ShutdownTimeout = TimeSpan.FromHours(1);
                }
                else
                {
                    o.ShutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(5);
                }
            });
    }
}
