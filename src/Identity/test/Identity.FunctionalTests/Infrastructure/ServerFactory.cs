// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class ServerFactory<TStartup,TContext>: WebApplicationFactory<TStartup>
        where TStartup : class
        where TContext : DbContext
    {
        private readonly SqliteConnection _connection
            = new SqliteConnection($"DataSource=:memory:");

        public ServerFactory()
        {
            _connection.Open();

            ClientOptions.AllowAutoRedirect = false;
            ClientOptions.BaseAddress = new Uri("https://localhost");
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            Program.UseStartup = false;
            return base.CreateHostBuilder();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseStartup<TStartup>();

            builder.ConfigureServices(sc =>
            {
                sc.SetupTestDatabase<TContext>(_connection)
                    .AddMvc()
                    // Mark the cookie as essential for right now, as Identity uses it on
                    // several places to pass important data in post-redirect-get flows.
                    .AddCookieTempDataProvider(o => o.Cookie.IsEssential = true);
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var result = base.CreateHost(builder);
            EnsureDatabaseCreated(result.Services);
            return result;
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            var result = base.CreateServer(builder);
            EnsureDatabaseCreated(result.Host.Services);
            return result;
        }

        public void EnsureDatabaseCreated(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                scope.ServiceProvider.GetService<TContext>()?.Database?.EnsureCreated();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _connection.Dispose();

            base.Dispose(disposing);
        }
    }
}
