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

var app = builder.Build();

if (!X509Utilities.HasCertificateType) {
    return 1;
}

return 100; // Success