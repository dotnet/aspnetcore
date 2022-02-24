// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Identity.DefaultUI.WebSite;

public class StartupBase<TUser, TContext>
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

        services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // This prevents running out of file watchers on some linux machines
        DisableFilePolling(env);

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
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
                case ManifestStaticWebAssetFileProvider manifestStaticWebAssets:
                    foreach (var provider in manifestStaticWebAssets.FileProviders)
                    {
                        pendingProviders.Push(provider);
                    }
                    break;
                case CompositeFileProvider composite:
                    foreach (var childFileProvider in composite.FileProviders)
                    {
                        pendingProviders.Push(childFileProvider);
                    }
                    break;
                case NullFileProvider:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown provider '{currentProvider.GetType().Name}'");
            }
        }
    }
}
