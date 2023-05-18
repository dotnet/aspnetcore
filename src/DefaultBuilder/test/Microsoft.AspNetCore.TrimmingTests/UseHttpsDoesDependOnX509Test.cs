using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000, listenOptions =>
    {
#pragma warning disable SYSLIB0026 // The constructor obsolete but we're not actually going to use the cert
        listenOptions.UseHttps(new X509Certificate2());
#pragma warning restore SYSLIB0026
    });
});

var app = builder.Build();

if (!X509Utilities.HasCertificateType) {
    return 1;
}

return 100; // Success