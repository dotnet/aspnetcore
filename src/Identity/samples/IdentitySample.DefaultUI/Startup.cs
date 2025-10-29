// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using IdentitySample.DefaultUI.Data;
using IdentitySample.DefaultUI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IdentitySample.DefaultUI;

public class BadDude : IUserConfirmation<ApplicationUser>
{
    public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        return Task.FromResult(false);
    }
}

public class Startup
{
    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

        builder.AddEnvironmentVariables();
        Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; set; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add framework services.
        services.AddDbContext<ApplicationDbContext>(
            options => options.ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning))
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
            x => x.MigrationsAssembly("IdentitySample.DefaultUI")));

        services.AddMvc().AddNewtonsoftJson();

        services.AddDefaultIdentity<ApplicationUser>(o =>
            {
                o.SignIn.RequireConfirmedAccount = true;
                // Configure Identity to use V3 schema with Id primary keys and unique indexes
                o.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
             .AddRoles<IdentityRole>()
             .AddEntityFrameworkStores<ApplicationDbContext>();

        // Add external authentication providers
        var authentication = services.AddAuthentication();

        // Google Authentication
        var googleClientId = Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = Configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SaveTokens = true;
            });
        }

        // Add email sender for account confirmation
        services.AddTransient<IEmailSender, EmailSender>();

        services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
        });
    }
}
