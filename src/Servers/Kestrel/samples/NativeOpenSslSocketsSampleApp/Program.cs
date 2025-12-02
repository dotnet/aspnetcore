// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure Kestrel to use the Direct Socket Transport with native OpenSSL integration
builder.WebHost.UseKestrelDirectSocket();

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint on port 5000
    // DirectSocket transport handles this connection
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    // HTTPS support will be added in future phases:
    // - Phase 2-3: Direct socket data flow through OpenSSL
    // - Phase 4: TLS handshake integration
    // - Phase 5: Error handling and diagnostics
    // - Phase 6: HttpsConnectionMiddleware replacement
});

var app = builder.Build();

app.MapGet("/hello", () => "hello!");

Console.WriteLine("Starting NativeOpenSslSocketsSampleApp with DirectSocket Transport");
Console.WriteLine();
Console.WriteLine("HTTP endpoint: http://localhost:5000/weatherforecast");
Console.WriteLine();
Console.WriteLine("DirectSocket Transport Features:");
Console.WriteLine("  ✓ Direct socket to application path (no SslStream wrapper)");
Console.WriteLine("  ✓ Bypasses HttpsConnectionMiddleware");
Console.WriteLine("  ✓ Foundation for zero-copy TLS processing");
Console.WriteLine("  ✓ Reduced memory allocations");
Console.WriteLine("  ✓ Lower latency connection handling");
Console.WriteLine();
Console.WriteLine("HTTPS support: Coming in future phases");
Console.WriteLine();

await app.RunAsync();
