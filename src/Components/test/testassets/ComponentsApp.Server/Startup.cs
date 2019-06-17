// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComponentsApp.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<CircuitHandler, LoggingCircuitHandler>();
            services.AddServerSideBlazor();
            //services.AddSignalR().AddAzureSignalR(o =>
            //{
            //    o.ServerStickyMode = ServerStickyMode.Required;
            //    o.ConnectionString = "Endpoint=https://antifoguertysignalr.service.signalr.net;AccessKey=vdGlVP/IMKLjlRbDOZX6v8Ce6XJ1o1OEayz+nWuTM3U=;Version=1.0;";
            //});

            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationOptions, TestScheme>("Test", o => { });
            services.AddSingleton<WeatherForecastService, DefaultWeatherForecastService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapBlazorHub()
                    .RequireAuthorization("Circuit");
                endpoints.MapFallbackToPage("/Index");
            });
        }
    }

    public class TestScheme : AuthenticationHandler<TestAuthenticationOptions>
    {
        public TestScheme(
            IOptionsMonitor<TestAuthenticationOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim("some", "claim") }, "Test")), "Test")));
        }
    }

    public class TestAuthenticationOptions : AuthenticationSchemeOptions
    {
    }
}
