// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
    public class DatabaseErrorPageMiddlewareTest
    {
        [Fact]
        public async Task Successful_requests_pass_thru()
        {
            var builder = new WebHostBuilder().Configure(app => app
                .UseDatabaseErrorPage()
                .UseMiddleware<SuccessMiddleware>());
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder().Configure(app => app
                .UseDatabaseErrorPage()
                .UseMiddleware<ExceptionMiddleware>());
            var server = new TestServer(builder);

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

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Existing_database_not_using_migrations_exception_passes_thru()
        {
            TestServer server = SetupTestServer<BloggingContext, DatabaseErrorButNoMigrationsMiddleware>();
            var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
                await server.CreateClient().GetAsync("http://localhost/"));

            Assert.Equal("Invalid column name 'Name'.", ex.InnerException.Message);
        }

        class DatabaseErrorButNoMigrationsMiddleware
        {
            public DatabaseErrorButNoMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContext>();
                db.Database.EnsureCreated();
                db.Database.ExecuteSqlCommand("ALTER TABLE dbo.Blog DROP COLUMN Name");

                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Error_page_displayed_no_migrations()
        {
            TestServer server = SetupTestServer<BloggingContext, NoMigrationsMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", typeof(BloggingContext).Name), content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_AddMigrationCommand").Replace(">", "&gt;"), content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_ApplyMigrationsCommand").Replace(">", "&gt;"), content);
        }

        class NoMigrationsMiddleware
        {
            public NoMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContext>();
                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Error_page_displayed_pending_migrations()
        {
            TestServer server = SetupTestServer<BloggingContextWithMigrations, PendingMigrationsMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingMigrationsTitle", typeof(BloggingContextWithMigrations).Name), content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_ApplyMigrationsCommand").Replace(">", "&gt;"), content);
            Assert.Contains("<li>111111111111111_MigrationOne</li>", content);
            Assert.Contains("<li>222222222222222_MigrationTwo</li>", content);

            Assert.DoesNotContain(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_AddMigrationCommand").Replace(">", "&gt;"), content);
        }

        class PendingMigrationsMiddleware
        {
            public PendingMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContextWithMigrations>();
                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Error_page_displayed_pending_model_changes()
        {
            TestServer server = SetupTestServer<BloggingContextWithPendingModelChanges, PendingModelChangesMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingChangesTitle", typeof(BloggingContextWithPendingModelChanges).Name), content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_AddMigrationCommand").Replace(">", "&gt;"), content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_ApplyMigrationsCommand").Replace(">", "&gt;"), content);
        }

        class PendingModelChangesMiddleware
        {
            public PendingModelChangesMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContextWithPendingModelChanges>();
                    db.Database.Migrate();

                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Error_page_then_apply_migrations()
        {
            TestServer server = SetupTestServer<BloggingContextWithMigrations, ApplyMigrationsMiddleware>();
            var client = server.CreateClient();

            var expectedMigrationsEndpoint = "/ApplyDatabaseMigrations";
            var expectedContextType = typeof(BloggingContextWithMigrations).AssemblyQualifiedName;

            // Step One: Initial request with database failure
            HttpResponseMessage response = await client.GetAsync("http://localhost/");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();

            // Ensure the url we're going to test is what the page is using in it's JavaScript
            var javaScriptEncoder = JavaScriptEncoder.Default;
            Assert.Contains("req.open(\"POST\", \"" + JavaScriptEncode(expectedMigrationsEndpoint) + "\", true);", content);
            Assert.Contains("var formBody = \"context=" + JavaScriptEncode(UrlEncode(expectedContextType)) + "\";", content);

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
                var db = context.RequestServices.GetService<BloggingContextWithMigrations>();
                db.Blogs.Add(new Blog());
                db.SaveChanges();
                await context.Response.WriteAsync("Saved a Blog");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Customize_migrations_end_point()
        {
            var migrationsEndpoint = "/MyCustomEndPoints/ApplyMyMigrationsHere";

            using (var database = SqlServerTestStore.CreateScratch())
            {
                var builder = new WebHostBuilder()
                    .Configure(app =>
                    {
                        app.UseDatabaseErrorPage(new DatabaseErrorPageOptions
                        {
                            MigrationsEndPointPath = new PathString(migrationsEndpoint)
                        });

                        app.UseMiddleware<PendingMigrationsMiddleware>();
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddAddEntityFrameworkSqlServer();
                        services.AddScoped<BloggingContextWithMigrations>();

                        var optionsBuilder = new DbContextOptionsBuilder();
                        optionsBuilder.UseSqlServer(database.ConnectionString);
                        services.AddSingleton<DbContextOptions>(optionsBuilder.Options);
                    });
                var server = new TestServer(builder);

                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("req.open(\"POST\", \"" + JavaScriptEncode(migrationsEndpoint) + "\", true);", content);
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Pass_thru_when_context_not_in_services()
        {
            using (var database = SqlServerTestStore.CreateScratch())
            {
                var logProvider = new TestLoggerProvider();

                var builder = new WebHostBuilder()
                    .Configure(app =>
                    {
                        app.UseDatabaseErrorPage();
                        app.UseMiddleware<ContextNotRegisteredInServicesMiddleware>();
                        app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
                    })
                    .ConfigureServices(
                    services =>
                    {
                        services.AddEntityFramework().AddSqlServer();
                        var optionsBuilder = new DbContextOptionsBuilder();
                        if (!PlatformHelper.IsMono)
                        {
                            optionsBuilder.UseSqlServer(database.ConnectionString);
                        }
                        else
                        {
                            optionsBuilder.UseInMemoryDatabase();
                        }
                        services.AddSingleton<DbContextOptions>(optionsBuilder.Options);
                    });
                var server = new TestServer(builder);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await server.CreateClient().GetAsync("http://localhost/"));

                Assert.True(logProvider.Logger.Messages.Any(m =>
                    m.StartsWith(StringsHelpers.GetResourceString("FormatDatabaseErrorPageMiddleware_ContextNotRegistered", typeof(BloggingContext)))));
            }
        }

        class ContextNotRegisteredInServicesMiddleware
        {
            public ContextNotRegisteredInServicesMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var options = context.RequestServices.GetService<DbContextOptions>();
                var db = new BloggingContext(options);
                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Pass_thru_when_exception_in_logic()
        {
            using (var database = SqlServerTestStore.CreateScratch())
            {
                var logProvider = new TestLoggerProvider();

                var server = SetupTestServer<BloggingContextWithSnapshotThatThrows, ExceptionInLogicMiddleware>(logProvider);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                    await server.CreateClient().GetAsync("http://localhost/"));

                Assert.True(logProvider.Logger.Messages.Any(m =>
                    m.StartsWith(StringsHelpers.GetResourceString("FormatDatabaseErrorPageMiddleware_Exception"))));
            }
        }

        class ExceptionInLogicMiddleware
        {
            public ExceptionInLogicMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContextWithSnapshotThatThrows>();
                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Error_page_displayed_when_exception_wrapped()
        {
            TestServer server = SetupTestServer<BloggingContext, WrappedExceptionMiddleware>();
            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("I wrapped your exception", content);
            Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", typeof(BloggingContext).Name), content);
        }

        class WrappedExceptionMiddleware
        {
            public WrappedExceptionMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContext>();
                db.Blogs.Add(new Blog());
                try
                {
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
                catch (Exception ex)
                {
                    throw new Exception("I wrapped your exception", ex);
                }
            }
        }

        private static TestServer SetupTestServer<TContext, TMiddleware>(ILoggerProvider logProvider = null)
            where TContext : DbContext
        {
            using (var database = SqlServerTestStore.CreateScratch())
            {
                var builder = new WebHostBuilder()
                    .Configure(app =>
                    {
                        app.UseDatabaseErrorPage();

                        app.UseMiddleware<TMiddleware>();

                        if (logProvider != null)
                        {
                            app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
                        }
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddEntityFramework()
                            .AddSqlServer();

                        services.AddScoped<TContext>();

                        var optionsBuilder = new DbContextOptionsBuilder();
                        if (!PlatformHelper.IsMono)
                        {
                            optionsBuilder.UseSqlServer(database.ConnectionString);
                        }
                        else
                        {
                            optionsBuilder.UseInMemoryDatabase();
                        }
                        services.AddSingleton(optionsBuilder.Options);
                    });
                return new TestServer(builder);
            }
        }

        private static UrlEncoder _urlEncoder = UrlEncoder.Default;
        private static string UrlEncode(string content)
        {
            return _urlEncoder.Encode(content);
        }

        private static JavaScriptEncoder _javaScriptEncoder = JavaScriptEncoder.Default;
        private static string JavaScriptEncode(string content)
        {
            return _javaScriptEncoder.Encode(content);
        }
    }
}