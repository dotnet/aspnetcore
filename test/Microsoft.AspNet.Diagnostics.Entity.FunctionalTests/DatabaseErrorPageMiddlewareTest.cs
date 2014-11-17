// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Entity.FunctionalTests.Helpers;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.TestHost;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.SqlServer.FunctionalTests;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class DatabaseErrorPageMiddlewareTest
    {
        [Fact]
        public async Task Successful_requests_pass_thru()
        {
            TestServer server = TestServer.Create(app => app
                .UseDatabaseErrorPage()
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
        public async Task Non_database_exceptions_pass_thru()
        {
            TestServer server = TestServer.Create(app => app
                .UseDatabaseErrorPage()
                .UseMiddleware<ExceptionMiddleware>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await server.CreateClient().GetAsync("http://localhost/"));

            Assert.Equal("Exception requested from TestMiddleware", ex.Message);
        }

        class ExceptionMiddleware
        {
            public ExceptionMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                throw new InvalidOperationException("Exception requested from TestMiddleware");
            }
        }

        [Fact]
        public async Task Error_page_displayed_no_migrations()
        {
            TestServer server = await SetupTestServer<BloggingContext, NoMigrationsMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(Strings.DatabaseErrorPage_NoDbOrMigrationsTitle(typeof(BloggingContext).Name), content);
            Assert.Contains(Strings.DatabaseErrorPage_AddMigrationCommand.Replace(">", "&gt;"), content);
            Assert.Contains(Strings.DatabaseErrorPage_ApplyMigrationsCommand.Replace(">", "&gt;"), content);
        }

        class NoMigrationsMiddleware
        {
            public NoMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                using (var db = context.ApplicationServices.GetService<BloggingContext>())
                {
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        [Fact]
        public async Task Error_page_displayed_pending_migrations()
        {
            TestServer server = await SetupTestServer<BloggingContextWithMigrations, PendingMigrationsMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(Strings.DatabaseErrorPage_PendingMigrationsTitle(typeof(BloggingContextWithMigrations).Name), content);
            Assert.Contains(Strings.DatabaseErrorPage_ApplyMigrationsCommand.Replace(">", "&gt;"), content);
            Assert.Contains("<li>111111111111111_MigrationOne</li>", content);
            Assert.Contains("<li>222222222222222_MigrationTwo</li>", content);

            Assert.DoesNotContain(Strings.DatabaseErrorPage_AddMigrationCommand.Replace(">", "&gt;"), content);
        }

        class PendingMigrationsMiddleware
        {
            public PendingMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                using (var db = context.ApplicationServices.GetService<BloggingContextWithMigrations>())
                {
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        [Fact]
        public async Task Error_page_displayed_pending_model_changes()
        {
            TestServer server = await SetupTestServer<BloggingContextWithPendingModelChanges, PendingModelChangesMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(Strings.DatabaseErrorPage_PendingChangesTitle(typeof(BloggingContextWithPendingModelChanges).Name), content);
            Assert.Contains(Strings.DatabaseErrorPage_AddMigrationCommand.Replace(">", "&gt;"), content);
            Assert.Contains(Strings.DatabaseErrorPage_ApplyMigrationsCommand.Replace(">", "&gt;"), content);
        }

        class PendingModelChangesMiddleware
        {
            public PendingModelChangesMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                using (var db = context.ApplicationServices.GetService<BloggingContextWithPendingModelChanges>())
                {
                    var services = (MigrationsDataStoreServices)db.Configuration.DataStoreServices;
                    services.Migrator.ApplyMigrations();

                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        [Fact]
        public async Task Error_page_then_apply_migrations()
        {
            TestServer server = await SetupTestServer<BloggingContextWithMigrations, ApplyMigrationsMiddleware>();
            var client = server.CreateClient();

            var expectedMigrationsEndpoint = "/ApplyDatabaseMigrations";
            var expectedContextType = typeof(BloggingContextWithMigrations).AssemblyQualifiedName;

            // Step One: Initial request with database failure
            HttpResponseMessage response = await client.GetAsync("http://localhost/");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();

            // Ensure the url we're going to test is what the page is using in it's JavaScript
            Assert.Contains("req.open(\"POST\", \"" + expectedMigrationsEndpoint + "\", true);", content);
            Assert.Contains("var params = \"context=\" + encodeURIComponent(\"" + expectedContextType + "\");", content);

            // Step Two: Request to migrations endpoint
            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("context", expectedContextType)
            });

            response = await client.PostAsync("http://localhost" + expectedMigrationsEndpoint, formData);
            content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Step Three: Successful request after migrations applied
            response = await client.GetAsync("http://localhost/");
            content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Saved a Blog", content);
        }

        class ApplyMigrationsMiddleware
        {
            public ApplyMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual async Task Invoke(HttpContext context)
            {
                using (var db = context.ApplicationServices.GetService<BloggingContextWithMigrations>())
                {
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    await context.Response.WriteAsync("Saved a Blog");
                }
            }
        }

        [Fact]
        public async Task Customize_migrations_end_point()
        {
            var migrationsEndpoint = "/MyCustomEndPoints/ApplyMyMigrationsHere";

            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                var server = TestServer.Create(app =>
                {
                    app.UseServices(services =>
                    {
                        services.AddEntityFramework().AddSqlServer();
                        services.AddScoped<BloggingContextWithMigrations>();
                        services.AddInstance<DbContextOptions>(new DbContextOptions().UseSqlServer(database.Connection.ConnectionString));
                    });

                    var options = DatabaseErrorPageOptions.ShowAll;
                    options.MigrationsEndPointPath = new PathString(migrationsEndpoint);
                    app.UseDatabaseErrorPage(options);

                    app.UseMiddleware<PendingMigrationsMiddleware>();
                });

                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("req.open(\"POST\", \"" + migrationsEndpoint + "\", true);", content);
            }
        }

        [Fact]
        public async Task Pass_thru_when_context_not_in_services()
        {
            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                var logProvider = new TestLoggerProvider();

                var server = TestServer.Create(app =>
                {
                    app.UseServices(services =>
                    {
                        services.AddEntityFramework()
                            .AddSqlServer();

                        services.AddInstance<DbContextOptions>(
                            new DbContextOptions()
                                .UseSqlServer(database.Connection.ConnectionString));
                    });

                    app.UseDatabaseErrorPage();

                    app.UseMiddleware<ContextNotRegisteredInServicesMiddleware>();

                    app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
                });

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await server.CreateClient().GetAsync("http://localhost/"));

                Assert.True(logProvider.Logger.Messages.Any(m =>
                    m.StartsWith(Strings.DatabaseErrorPageMiddleware_ContextNotRegistered(typeof(BloggingContext)))));
            }
        }

        class ContextNotRegisteredInServicesMiddleware
        {
            public ContextNotRegisteredInServicesMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var options = context.ApplicationServices.GetService<DbContextOptions>();
                using (var db = new BloggingContext(context.ApplicationServices, options))
                {
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        [Fact]
        public async Task Pass_thru_when_exception_in_logic()
        {
            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                var logProvider = new TestLoggerProvider();

                var server = await SetupTestServer<BloggingContextWithSnapshotThatThrows, ExceptionInLogicMiddleware>(logProvider);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await server.CreateClient().GetAsync("http://localhost/"));

                Assert.True(logProvider.Logger.Messages.Any(m =>
                    m.StartsWith(Strings.DatabaseErrorPageMiddleware_Exception)));
            }
        }

        class ExceptionInLogicMiddleware
        {
            public ExceptionInLogicMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                using (var db = context.ApplicationServices.GetService<BloggingContextWithSnapshotThatThrows>())
                {
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        private static async Task<TestServer> SetupTestServer<TContext, TMiddleware>(ILoggerProvider logProvider = null)
            where TContext : DbContext
        {
            using (var database = await SqlServerTestStore.CreateScratchAsync(createDatabase: false))
            {
                return TestServer.Create(app =>
                {
                    app.UseServices(services =>
                    {
                        services.AddEntityFramework()
                            .AddSqlServer();

                        services.AddScoped<TContext>();
                        services.AddInstance<DbContextOptions>(
                            new DbContextOptions()
                                .UseSqlServer(database.Connection.ConnectionString));
                    });

                    app.UseDatabaseErrorPage();

                    app.UseMiddleware<TMiddleware>();

                    if (logProvider != null)
                    {
                        app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
                    }
                });
            }
        }
    }
}