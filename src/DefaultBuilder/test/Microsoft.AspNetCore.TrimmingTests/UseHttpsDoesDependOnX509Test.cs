// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

try
{
    _ = builder.Build();
}
catch (InvalidOperationException)
{
    // Expected if there's no dev cert installed
}

if (!X509Utilities.HasCertificateType)
{
    return -1;
}

return 100; // Success