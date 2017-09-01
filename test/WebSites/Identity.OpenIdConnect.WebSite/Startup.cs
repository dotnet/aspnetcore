// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.OpenIdConnect.WebSite.Identity.Data;
using Identity.OpenIdConnect.WebSite.Identity.Models;
using Identity.OpenIdConnect.WebSite.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service.Extensions;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.OpenIdConnect.WebSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<IdentityServiceDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            var builder = services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddDefaultTokenProviders()
                .AddApplications<ApplicationUser, IdentityServiceApplication>()
                .AddEntityFrameworkStores<IdentityServiceDbContext>()
                .AddClientInfoBinding();

            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddOpenIdConnect()
            .AddCookie();

            services.WithIntegratedWebClient(options => options.ClientId = "56A33E6A-ADFE-47EA-BBFE-40F4AE4C55BA");

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Use(async (ctx, next) =>
            {
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                //app.UseDevelopmentCertificateErrorPage(Configuration);
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            var port = Configuration.GetValue<int?>("Https:Port", null);
            var rewriteOptions = new RewriteOptions();
            rewriteOptions.AddRedirectToHttps(StatusCodes.Status302Found, port);
            app.UseRewriter(rewriteOptions);

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
