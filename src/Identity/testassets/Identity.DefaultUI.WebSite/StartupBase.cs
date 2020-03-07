// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Identity.DefaultUI.WebSite
{
    public class StartupBase<TUser,TContext>
        where TUser : class
        where TContext : DbContext
    {
        public StartupBase(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            services.AddDbContext<TContext>(options =>
                options
                    .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning))
                    //.UseSqlServer(
                    //    Configuration.GetConnectionString("DefaultConnection"),
                    //    sqlOptions => sqlOptions.MigrationsAssembly("Identity.DefaultUI.WebSite")
                    //));
                    .UseSqlite("DataSource=:memory:"));

            services.AddDefaultIdentity<TUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<TContext>();
                
            services.AddMvc();
            services.AddSingleton<IFileVersionProvider, FileVersionProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // This prevents running out of file watchers on some linux machines
            DisableFilePolling(env);
        
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }

        public static void DisableFilePolling(IWebHostEnvironment env)
        {
            var pendingProviders = new Stack<IFileProvider>();
            pendingProviders.Push(env.WebRootFileProvider);
            pendingProviders.Push(env.ContentRootFileProvider);
            while (pendingProviders.TryPop(out var currentProvider))
            {
                switch (currentProvider)
                {
                    case PhysicalFileProvider physical:
                        physical.UseActivePolling = false;
                        break;
                    case IFileProvider staticWebAssets when staticWebAssets.GetType().Name == "StaticWebAssetsFileProvider":
                        GetUnderlyingProvider(staticWebAssets).UseActivePolling = false;
                        break;
                    case CompositeFileProvider composite:
                        foreach (var childFileProvider in composite.FileProviders)
                        {
                            pendingProviders.Push(childFileProvider);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unknown provider");
                }
            }
        }

        private static PhysicalFileProvider GetUnderlyingProvider(IFileProvider staticWebAssets)
        {
            return (PhysicalFileProvider) staticWebAssets.GetType().GetProperty("InnerProvider").GetValue(staticWebAssets);
        }
    }
}
