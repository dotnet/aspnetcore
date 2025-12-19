// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var withCustomDirectTransport = true;

var builder = WebApplication.CreateSlimBuilder(args);

if (withCustomDirectTransport)
{
    // Configure Kestrel to use the Direct Socket Transport. It by-passes the HttpsMiddleware and SslStream
    builder.WebHost.UseKestrelDirectSslTransport();

    builder.WebHost.UseDirectSslSockets(options =>
    {
        options.CertificatePath = "server-p384.crt";
        options.PrivateKeyPath = "server-p384.key";

        options.WorkerCount = 1;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        // HTTPS endpoint on port 5001 with DirectSocket + OpenSSL
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}
else
{
    // Disable verbose logging for better benchmark performance
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    // Configure Kestrel to use the default Sockets Transport with SslStream
    builder.WebHost.UseKestrel(options =>
    {
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.UseHttps(new X509Certificate2("server-p384.pfx", "testpassword"));
        });
    });
}

var app = builder.Build();

app.MapGet("/", (HttpContext ctx) =>
{
    return "Hello world";
});

await app.RunAsync();