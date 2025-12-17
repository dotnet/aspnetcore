// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure Kestrel to use the Direct Socket Transport. It by-passes the HttpsMiddleware and SslStream
builder.WebHost.UseKestrelDirectSslTransport();

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint on port 5000
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });

    // HTTPS endpoint on port 5001 with DirectSocket + OpenSSL
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

builder.WebHost.UseDirectSslSockets(options => {
   options.CertificatePath = "server-p384.crt";
   options.PrivateKeyPath = "server-p384.key";
});

var app = builder.Build();

app.MapGet("/", () => "hello world!");

await app.RunAsync();