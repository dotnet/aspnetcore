// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Http.Extensions;

namespace Certificate.Optional.Sample;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                options.Events = new CertificateAuthenticationEvents()
                {
                    // If there is no certificate we must be on HostWithoutCert that does not require one. Redirect to HostWithCert to prompt for a certificate.
                    OnChallenge = context =>
                    {
                        var request = context.Request;
                        var redirect = UriHelper.BuildAbsolute("https",
                            new HostString(Program.HostWithCert, context.HttpContext.Connection.LocalPort),
                            request.PathBase, request.Path, request.QueryString);
                        context.Response.Redirect(redirect, permanent: false, preserveMethod: true);
                        context.HandleResponse(); // Don't do the default behavior that would send a 403 response.
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Map("/auth", context =>
            {
                return context.Response.WriteAsync($"Hello {context.User.Identity.Name} at {context.Request.Host}");
            }).RequireAuthorization();

            endpoints.Map("{*url}", context =>
            {
                return context.Response.WriteAsync($"Hello {context.User.Identity.Name} at {context.Request.Host}. Try /auth");
            });
        });
    }
}
