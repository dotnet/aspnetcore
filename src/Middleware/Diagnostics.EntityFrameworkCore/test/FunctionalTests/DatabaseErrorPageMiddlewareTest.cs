// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
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
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Request Handled");
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Existing_database_not_using_migrations_exception_passes_thru()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContext, DatabaseErrorButNoMigrationsMiddleware>(database);
                var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
                    await server.CreateClient().GetAsync("http://localhost/"));

                Assert.Equal("SQLite Error 1: 'no such table: Blogs'.", ex.InnerException.Message);
            }
        }

        class DatabaseErrorButNoMigrationsMiddleware
        {
            public DatabaseErrorButNoMigrationsMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                var db = context.RequestServices.GetService<BloggingContext>();
                db.Database.EnsureCreated();
                db.Database.ExecuteSqlRaw("ALTER TABLE Blogs RENAME TO Bloogs");

                db.Blogs.Add(new Blog());
                db.SaveChanges();
                throw new Exception("SaveChanges should have thrown");
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Error_page_displayed_no_migrations()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContext, NoMigrationsMiddleware>(database);
                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", typeof(BloggingContext).Name), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_AddMigrationCommandPMC").Replace(">", "&gt;"), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_ApplyMigrationsCommandPMC").Replace(">", "&gt;"), content);
            }
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void No_exception_on_diagnostic_event_received_when_null_state()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                using (var server = SetupTestServer<BloggingContext, NoMigrationsMiddleware>(database))
                {
                    using (var db = server.Host.Services.GetService<BloggingContext>())
                    {
                        db.Blogs.Add(new Blog());

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Assert.Equal("DbUpdateException", e.GetType().Name);
                        }
                    }
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Error_page_displayed_pending_migrations()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContextWithMigrations, PendingMigrationsMiddleware>(database);
                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingMigrationsTitle", typeof(BloggingContextWithMigrations).Name), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_ApplyMigrationsCommandPMC").Replace(">", "&gt;"), content);
                Assert.Contains("<li>111111111111111_MigrationOne</li>", content);
                Assert.Contains("<li>222222222222222_MigrationTwo</li>", content);

                Assert.DoesNotContain(StringsHelpers.GetResourceString("DatabaseErrorPage_AddMigrationCommandPMC").Replace(">", "&gt;"), content);
            }
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Error_page_displayed_pending_model_changes()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContextWithPendingModelChanges, PendingModelChangesMiddleware>(database);
                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_PendingChangesTitle", typeof(BloggingContextWithPendingModelChanges).Name), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_AddMigrationCommandCLI").Replace(">", "&gt;"), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_AddMigrationCommandPMC").Replace(">", "&gt;"), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_ApplyMigrationsCommandCLI").Replace(">", "&gt;"), content);
                Assert.Contains(StringsHelpers.GetResourceString("DatabaseErrorPage_ApplyMigrationsCommandPMC").Replace(">", "&gt;"), content);
            }
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Error_page_then_apply_migrations()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContextWithMigrations, ApplyMigrationsMiddleware>(database);
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Customize_migrations_end_point()
        {
            var migrationsEndpoint = "/MyCustomEndPoints/ApplyMyMigrationsHere";

            using (var database = SqlTestStore.CreateScratch())
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
                        services.AddDbContext<BloggingContextWithMigrations>(
                            optionsBuilder => optionsBuilder.UseSqlite(database.ConnectionString));
                    });
                var server = new TestServer(builder);

                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("req.open(\"POST\", \"" + JavaScriptEncode(migrationsEndpoint) + "\", true);", content);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Pass_thru_when_context_not_in_services()
        {
            var logProvider = new TestLoggerProvider();

            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseDatabaseErrorPage();
                    app.UseMiddleware<ContextNotRegisteredInServicesMiddleware>();
#pragma warning disable CS0618 // Type or member is obsolete
                    app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
#pragma warning restore CS0618 // Type or member is obsolete
                });
            var server = new TestServer(builder);

            try
            {
                await server.CreateClient().GetAsync("http://localhost/");
            }
            catch (Exception exception)
            {
                Assert.True(
                    exception.GetType().Name == "SqliteException"
                    || exception.InnerException?.GetType().Name == "SqliteException");
            }

            Assert.Contains(logProvider.Logger.Messages.ToList(), m =>
                m.StartsWith(StringsHelpers.GetResourceString("FormatDatabaseErrorPageMiddleware_ContextNotRegistered", typeof(BloggingContext))));
        }

        class ContextNotRegisteredInServicesMiddleware
        {
            public ContextNotRegisteredInServicesMiddleware(RequestDelegate next)
            { }

            public virtual Task Invoke(HttpContext context)
            {
                using (var database = SqlTestStore.CreateScratch())
                {
                    var optionsBuilder = new DbContextOptionsBuilder()
                        .UseLoggerFactory(context.RequestServices.GetService<ILoggerFactory>())
                        .UseSqlite(database.ConnectionString);

                    var db = new BloggingContext(optionsBuilder.Options);
                    db.Blogs.Add(new Blog());
                    db.SaveChanges();
                    throw new Exception("SaveChanges should have thrown");
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Pass_thru_when_exception_in_logic()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                var logProvider = new TestLoggerProvider();

                var server = SetupTestServer<BloggingContextWithSnapshotThatThrows, ExceptionInLogicMiddleware>(database, logProvider);

                try
                {
                    await server.CreateClient().GetAsync("http://localhost/");
                }
                catch (Exception exception)
                {
                    Assert.True(
                        exception.GetType().Name == "SqliteException"
                        || exception.InnerException?.GetType().Name == "SqliteException");
                }

                Assert.Contains(logProvider.Logger.Messages.ToList(), m =>
                    m.StartsWith(StringsHelpers.GetResourceString("DatabaseErrorPageMiddleware_Exception")));
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task Error_page_displayed_when_exception_wrapped()
        {
            using (var database = SqlTestStore.CreateScratch())
            {
                TestServer server = SetupTestServer<BloggingContext, WrappedExceptionMiddleware>(database);
                HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/");

                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("I wrapped your exception", content);
                Assert.Contains(StringsHelpers.GetResourceString("FormatDatabaseErrorPage_NoDbOrMigrationsTitle", typeof(BloggingContext).Name), content);
            }
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

        private static TestServer SetupTestServer<TContext, TMiddleware>(SqlTestStore database, ILoggerProvider logProvider = null)
            where TContext : DbContext
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseDatabaseErrorPage();

                    app.UseMiddleware<TMiddleware>();

                    if (logProvider != null)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        app.ApplicationServices.GetService<ILoggerFactory>().AddProvider(logProvider);
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddDbContext<TContext>(optionsBuilder => optionsBuilder.UseSqlite(database.ConnectionString));
                });

            return new TestServer(builder);
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
