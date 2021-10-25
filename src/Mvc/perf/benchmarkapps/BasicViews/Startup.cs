// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
#if GENERATE_SQL_SCRIPTS
using System.Linq;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BasicViews
{
    public class Startup
    {
        private bool _isSQLite;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration["ConnectionString"];
            var databaseType = Configuration["Database"];
            if (string.IsNullOrEmpty(databaseType))
            {
                // Use SQLite when running outside a benchmark test or if benchmarks user specified "None".
                // ("None" is not passed to the web application.)
                databaseType = "SQLite";
            }
            else if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string must be specified for {databaseType}.");
            }

            switch (databaseType.ToUpperInvariant())
            {
#if !NETFRAMEWORK
                case "MYSQL":
                    services
                        .AddEntityFrameworkMySql()
                        .AddDbContextPool<BasicViewsContext>(options => options.UseMySql(connectionString));
                    break;
#endif

                case "POSTGRESQL":
                    var settings = new NpgsqlConnectionStringBuilder(connectionString);
                    if (!settings.NoResetOnClose)
                    {
                        throw new ArgumentException("No Reset On Close=true must be specified for Npgsql.");
                    }
                    if (settings.Enlist)
                    {
                        throw new ArgumentException("Enlist=false must be specified for Npgsql.");
                    }

                    services
                        .AddEntityFrameworkNpgsql()
                        .AddDbContextPool<BasicViewsContext>(options => options.UseNpgsql(connectionString));
                    break;

                case "SQLITE":
                    _isSQLite = true;
                    services
                        .AddEntityFrameworkSqlite()
                        .AddDbContextPool<BasicViewsContext>(options => options.UseSqlite("Data Source=BasicViews.db;Cache=Shared"));
                    break;

                case "SQLSERVER":
                    services
                        .AddEntityFrameworkSqlServer()
                        .AddDbContextPool<BasicViewsContext>(options => options.UseSqlServer(connectionString));
                    break;

                default:
                    throw new ArgumentException($"Application does not support database type {databaseType}.");
            }

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime lifetime)
        {
            var services = app.ApplicationServices;
            CreateDatabaseTables(services);
            if (_isSQLite)
            {
                lifetime.ApplicationStopping.Register(() => DropDatabase(services));
            }
            else
            {
                lifetime.ApplicationStopping.Register(() => DropDatabaseTables(services));
            }

            app.Use(next => async context =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void CreateDatabaseTables(IServiceProvider services)
        {
            using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<BasicViewsContext>())
                {
#if GENERATE_SQL_SCRIPTS
                    var migrator = dbContext.GetService<IMigrator>();
                    var script = migrator.GenerateScript(
                        fromMigration: Migration.InitialDatabase,
                        toMigration: dbContext.Database.GetMigrations().LastOrDefault());
                    Console.WriteLine("Create script:");
                    Console.WriteLine(script);
#endif

                    dbContext.Database.Migrate();
                }
            }
        }

        // Don't leave SQLite's .db file behind.
        public static void DropDatabase(IServiceProvider services)
        {
            using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<BasicViewsContext>())
                {
#if GENERATE_SQL_SCRIPTS
                    var migrator = dbContext.GetService<IMigrator>();
                    var script = migrator.GenerateScript(
                        fromMigration: dbContext.Database.GetAppliedMigrations().LastOrDefault(),
                        toMigration: Migration.InitialDatabase);
                    Console.WriteLine("Delete script:");
                    Console.WriteLine(script);
#endif

                    dbContext.Database.EnsureDeleted();
                }
            }
        }

        private void DropDatabaseTables(IServiceProvider services)
        {
            using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<BasicViewsContext>())
                {
                    var migrator = dbContext.GetService<IMigrator>();
#if GENERATE_SQL_SCRIPTS
                    var script = migrator.GenerateScript(
                        fromMigration: dbContext.Database.GetAppliedMigrations().LastOrDefault(),
                        toMigration: Migration.InitialDatabase);
                    Console.WriteLine("Delete script:");
                    Console.WriteLine(script);
#endif

                    migrator.Migrate(Migration.InitialDatabase);
                }
            }
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://+:5000")
                .UseConfiguration(configuration)
                .UseIISIntegration()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
        }
    }
}
