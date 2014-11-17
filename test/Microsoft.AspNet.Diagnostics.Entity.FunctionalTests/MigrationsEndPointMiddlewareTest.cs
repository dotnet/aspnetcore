// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.SqlServer.FunctionalTests;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class MigrationsEndPointMiddlewareTest
    {
        [Fact]
        public async Task Non_migration_requests_pass_thru()
        {
            TestServer server = TestServer.Create(app => app
                .UseMigrationsEndPoint()
                .UseMiddleware<SuccessMiddleware>());

            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal("Request Handled", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        class SuccessMiddleware
        {
            public SuccessMiddleware(RequestDelegate next)
            { }

            public virtual async Task Invoke(HttpContext context)
            {
                await context.Response.WriteAsync("Request Handled");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
        }

        [Fact]
        public async Task Migration_request_default_path()
        {
            await Migration_request(useCustomPath: false);
        }

        [Fact]
        public async Task Migration_request_custom_path()
        {
            await Migration_request(useCustomPath: true);
        }

        private async Task Migration_request(bool useCustomPath)
        {
            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                var options = new DbContextOptions().UseSqlServer(database.Connection.ConnectionString);
                var path = useCustomPath ? new PathString("/EndPoints/ApplyMyMigrations") : MigrationsEndPointOptions.DefaultPath;

                TestServer server = TestServer.Create(app =>
                {
                    app.UseServices(services =>
                        {
                            services.AddEntityFramework().AddSqlServer();
                            services.AddScoped<BloggingContextWithMigrations>();
                            services.AddInstance<DbContextOptions>(options);
                        });

                    if (useCustomPath)
                    {
                        app.UseMigrationsEndPoint(new MigrationsEndPointOptions { Path = path });
                    }
                    else
                    {
                        app.UseMigrationsEndPoint();
                    }
                });

                using (var db = BloggingContextWithMigrations.CreateWithoutExternalServiceProvider(options))
                {
                    Assert.False(db.Database.AsRelational().Exists());

                    var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithMigrations).AssemblyQualifiedName)
                    });

                    HttpResponseMessage response = await server.CreateClient()
                        .PostAsync("http://localhost" + path, formData);

                    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

                    Assert.True(db.Database.AsRelational().Exists());
                    var services = (MigrationsDataStoreServices)db.Configuration.DataStoreServices;
                    var appliedMigrations = services.Migrator.GetDatabaseMigrations();
                    Assert.Equal(2, appliedMigrations.Count);
                    Assert.Equal("111111111111111_MigrationOne", appliedMigrations.ElementAt(0).GetMigrationId());
                    Assert.Equal("222222222222222_MigrationTwo", appliedMigrations.ElementAt(1).GetMigrationId());
                }
            }
        }

        [Fact]
        public async Task Context_type_not_specified()
        {
            var server = TestServer.Create(app =>
            {
                app.UseMigrationsEndPoint();
            });

            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());

            var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.StartsWith(Strings.MigrationsEndPointMiddleware_NoContextType, content);
            Assert.True(content.Length > 512);
        }

        [Fact]
        public async Task Invalid_context_type_specified()
        {
            var server = TestServer.Create(app =>
            {
                app.UseMigrationsEndPoint();
            });

            var typeName = "You won't find this type ;)";
            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeName)
                });

            var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.StartsWith(Strings.MigrationsEndPointMiddleware_InvalidContextType(typeName), content);
            Assert.True(content.Length > 512);
        }

        [Fact]
        public async Task Context_not_registered_in_services()
        {
            var server = TestServer.Create(app =>
            {
                app.UseServices(services =>
                {
                    services.AddEntityFramework().AddSqlServer();
                });
                app.UseMigrationsEndPoint();
            });

            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeof(BloggingContext).AssemblyQualifiedName)
                });

            var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.StartsWith(Strings.MigrationsEndPointMiddleware_ContextNotRegistered(typeof(BloggingContext)), content);
            Assert.True(content.Length > 512);
        }

        [Fact]
        public async Task Exception_while_applying_migrations()
        {
            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                var options = new DbContextOptions().UseSqlServer(database.Connection.ConnectionString);

                TestServer server = TestServer.Create(app =>
                {
                    app.UseServices(services =>
                    {
                        services.AddEntityFramework().AddSqlServer();
                        services.AddScoped<BloggingContextWithSnapshotThatThrows>();
                        services.AddInstance<DbContextOptions>(options);
                    });

                    app.UseMigrationsEndPoint();
                });

                var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithSnapshotThatThrows).AssemblyQualifiedName)
                    });

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData));

                Assert.Equal(Strings.MigrationsEndPointMiddleware_Exception(typeof(BloggingContextWithSnapshotThatThrows)), ex.Message);
                Assert.Equal("Welcome to the invalid snapshot!", ex.InnerException.Message);
            }
        }
    }
}