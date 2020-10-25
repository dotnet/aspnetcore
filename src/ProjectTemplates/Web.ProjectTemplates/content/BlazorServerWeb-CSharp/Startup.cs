using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
#endif
#if (OrganizationalAuth)
#if (MultiOrgAuth)
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
#endif
using Microsoft.AspNetCore.Hosting;
#if (RequiresHttps)
using Microsoft.AspNetCore.HttpsPolicy;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if(MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif
#if (IndividualLocalAuth)
using BlazorServerWeb_CSharp.Areas.Identity;
#endif
using BlazorServerWeb_CSharp.Data;
#if (GenerateGraph)
using Microsoft.Graph;
#endif

namespace BlazorServerWeb_CSharp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
#if (IndividualLocalAuth)
            services.AddDbContext<ApplicationDbContext>(options =>
#if (UseLocalDB)
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
#else
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
#endif
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
#elif (OrganizationalAuth)
#if (GenerateApiOrGraph)
            var initialScopes = Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

#endif
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
#if (GenerateApiOrGraph)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
#if (GenerateApi)
                        .AddDownstreamWebApi("DownstreamApi", Configuration.GetSection("DownstreamApi"))
#endif
#if (GenerateGraph)
                        .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
#endif
                        .AddInMemoryTokenCaches();
#else
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));
#endif
#elif (IndividualB2CAuth)
#if (GenerateApi)
            var initialScopes = Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

#endif
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
#if (GenerateApi)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"))
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                        .AddDownstreamWebApi("DownstreamApi", Configuration.GetSection("DownstreamApi"))
                        .AddInMemoryTokenCaches();
#else
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"));
#endif
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
            services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();

            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });

#endif
            services.AddRazorPages();
#if (OrganizationalAuth || IndividualB2CAuth)
            services.AddServerSideBlazor()
                .AddMicrosoftIdentityConsentHandler();
#else
            services.AddServerSideBlazor();
#endif
#if (IndividualLocalAuth)
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            services.AddDatabaseDeveloperPageExceptionFilter();
#endif
            services.AddSingleton<WeatherForecastService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
                app.UseMigrationsEndPoint();
#endif
            }
            else
            {
                app.UseExceptionHandler("/Error");
#if (RequiresHttps)
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
#else
            }

#endif
            app.UseStaticFiles();

            app.UseRouting();

#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();
            app.UseAuthorization();

#endif
            app.UseEndpoints(endpoints =>
            {
#if (OrganizationalAuth || IndividualAuth)
                endpoints.MapControllers();
#endif
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
