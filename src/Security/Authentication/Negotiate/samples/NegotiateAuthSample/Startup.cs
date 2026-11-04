// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Negotiate;

namespace NegotiateAuthSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = options.DefaultPolicy;
        });
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate(options =>
            {
                if (OperatingSystem.IsLinux())
                {
                    /*
                    options.EnableLdap("DOMAIN.net");

                    options.EnableLdap(settings =>
                    {
                        // Mandatory settings
                        settings.Domain = "DOMAIN.com";
                        // Optional settings
                        settings.MachineAccountName = "machineName";
                        settings.MachineAccountPassword = "PassW0rd";
                        settings.IgnoreNestedGroups = true;
                    });
                    */
                }

                options.Events = new NegotiateEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        // context.SkipHandler();
                        return Task.CompletedTask;
                    }
                };
            });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseDeveloperExceptionPage();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Run(HandleRequest);
    }

    public async Task HandleRequest(HttpContext context)
    {
        var user = context.User.Identity;
        await context.Response.WriteAsync($"Authenticated? {user.IsAuthenticated}, Name: {user.Name}, Protocol: {context.Request.Protocol}");
    }
}
