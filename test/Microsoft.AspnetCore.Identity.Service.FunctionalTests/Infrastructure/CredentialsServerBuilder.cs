// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Identity.OpenIdConnect.WebSite;
using Identity.OpenIdConnect.WebSite.Identity.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    public class CredentialsServerBuilder
    {
        private readonly DelegatingHandler _loopBackHandler = new LoopBackHandler();

        public CredentialsServerBuilder()
        {
            Server = new MvcWebApplicationBuilder<Startup>()
                .UseSolutionRelativeContentRoot(@"./test/WebSites/Identity.OpenIdConnect.WebSite")
                .UseApplicationAssemblies();
        }

        public CredentialsServerBuilder ConfigureReferenceData(Action<ReferenceData> action)
        {
            var referenceData = new ReferenceData();
            action(referenceData);
            Server.ConfigureBeforeStartup(s => s.TryAddSingleton(referenceData));

            return this;
        }

        public CredentialsServerBuilder ConfigureInMemoryEntityFrameworkStorage(string dbName = "test")
        {
            Server.ConfigureBeforeStartup(services =>
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, EntityFrameworkSeedReferenceData>());
                services.AddDbContext<IdentityServiceDbContext>(options =>
                    options.UseInMemoryDatabase("test", memoryOptions => { }));
            });

            return this;
        }

        public CredentialsServerBuilder ConfigureMvcAutomaticSignIn()
        {
            Server.ConfigureBeforeStartup(s => s.Configure<MvcOptions>(o => o.Filters.Add(new AutoSignInFilter())));
            return this;
        }

        public CredentialsServerBuilder ConfigureOpenIdConnectClient(Action<OpenIdConnectOptions> action)
        {
            Server.ConfigureAfterStartup(services =>
            {
                services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.BackchannelHttpHandler = _loopBackHandler;
                    options.CorrelationCookie.Path = "/";
                    options.NonceCookie.Path = "/";
                });

                services.Configure(OpenIdConnectDefaults.AuthenticationScheme, action);
            });

            return this;
        }

        public CredentialsServerBuilder ConfigureIntegratedClient(string clientId)
        {
            Server.ConfigureAfterStartup(services =>
            {
                services.Configure<IntegratedWebClientOptions>(options => options.ClientId = clientId);
            });

            return this;
        }

        public CredentialsServerBuilder EnsureDeveloperCertificate()
        {
            Server.ConfigureBeforeStartup(services => services.Configure<IdentityServiceOptions>(
                o => o.SigningKeys.Add(
                    new SigningCredentials(
                        new X509SecurityKey(new X509Certificate2("./test-cert.pfx", "test")), "RS256"))));

            return this;
        }

        public MvcWebApplicationBuilder<Startup> Server { get; }

        public HttpClient Build()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var host = Server.Build();
            host.BaseAddress = new Uri("https://localhost");

            var clientHandler = host.CreateHandler();
            _loopBackHandler.InnerHandler = clientHandler;

            var cookieHandler = new CookieContainerHandler(clientHandler);

            var client = new HttpClient(cookieHandler);
            client.BaseAddress = new Uri("https://localhost");

            return client;
        }
    }
}
