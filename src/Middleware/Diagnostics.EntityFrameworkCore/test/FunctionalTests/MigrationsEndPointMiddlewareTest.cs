// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests;

public class MigrationsEndPointMiddlewareTest
{
    [Fact]
    public async Task Non_migration_requests_pass_thru()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app
                .UseMigrationsEndPoint()
                .UseMiddleware<SuccessMiddleware>());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

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

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Migration_request_default_path()
    {
        await Migration_request(useCustomPath: false);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Migration_request_custom_path()
    {
        await Migration_request(useCustomPath: true);
    }

    private async Task Migration_request(bool useCustomPath)
    {
        using (var database = SqlTestStore.CreateScratch())
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite(database.ConnectionString);

            var path = useCustomPath ? new PathString("/EndPoints/ApplyMyMigrations") : MigrationsEndPointOptions.DefaultPath;

            using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    if (useCustomPath)
                    {
                        app.UseMigrationsEndPoint(new MigrationsEndPointOptions
                        {
                            Path = path
                        });
                    }
                    else
                    {
                        app.UseMigrationsEndPoint();
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddDbContext<BloggingContextWithMigrations>(options =>
                    {
                        options.UseSqlite(database.ConnectionString);
                    });
                });
            }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();

            using (var db = BloggingContextWithMigrations.CreateWithoutExternalServiceProvider(optionsBuilder.Options))
            {
                var databaseCreator = db.GetService<IRelationalDatabaseCreator>();
                Assert.False(databaseCreator.Exists());

                var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithMigrations).AssemblyQualifiedName)
                    });

                HttpResponseMessage response = await server.CreateClient()
                    .PostAsync("http://localhost" + path, formData);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

                Assert.True(databaseCreator.Exists());

                var historyRepository = db.GetService<IHistoryRepository>();
                var appliedMigrations = historyRepository.GetAppliedMigrations();
                Assert.Equal(2, appliedMigrations.Count);
                Assert.Equal("111111111111111_MigrationOne", appliedMigrations.ElementAt(0).MigrationId);
                Assert.Equal("222222222222222_MigrationTwo", appliedMigrations.ElementAt(1).MigrationId);
            }
        }
    }

    [Fact]
    public async Task Context_type_not_specified()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMigrationsEndPoint();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("MigrationsEndPointMiddleware_NoContextType"), content);
        Assert.True(content.Length > 512);
    }

    [Fact]
    public async Task Invalid_context_type_specified()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMigrationsEndPoint();
                });
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var typeName = "You won't find this type ;)";
        var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeName)
                });

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_ContextNotRegistered", typeName), content);
        Assert.True(content.Length > 512);
    }

    [Fact]
    public async Task Context_not_registered_in_services()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseMigrationsEndPoint())
                .ConfigureServices(services => services.AddEntityFrameworkSqlite());
            }).Build();

        await host.StartAsync();

        var server = host.GetTestServer();

        var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("context", typeof(BloggingContext).AssemblyQualifiedName)
                });

        var response = await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_ContextNotRegistered", typeof(BloggingContext).AssemblyQualifiedName), content);
        Assert.True(content.Length > 512);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public async Task Exception_while_applying_migrations()
    {
        using (var database = SqlTestStore.CreateScratch())
        {
            using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app => app.UseMigrationsEndPoint())
                .ConfigureServices(services =>
                {
                    services.AddDbContext<BloggingContextWithSnapshotThatThrows>(optionsBuilder =>
                    {
                        optionsBuilder.UseSqlite(database.ConnectionString);
                    });
                });
            }).Build();

            await host.StartAsync();

            var server = host.GetTestServer();

            var formData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("context", typeof(BloggingContextWithSnapshotThatThrows).AssemblyQualifiedName)
                    });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await server.CreateClient().PostAsync("http://localhost" + MigrationsEndPointOptions.DefaultPath, formData));

            Assert.StartsWith(StringsHelpers.GetResourceString("FormatMigrationsEndPointMiddleware_Exception", typeof(BloggingContextWithSnapshotThatThrows)), ex.Message);
            Assert.Equal("Welcome to the invalid snapshot!", ex.InnerException.Message);
        }
    }
}
