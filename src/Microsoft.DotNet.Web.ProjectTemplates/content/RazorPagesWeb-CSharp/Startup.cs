using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#endif
#if (OrganizationalAuth || IndividualAuth)
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if (OrganizationalAuth && OrgReadAccess)
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
#endif
#if (MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif

namespace Company.WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

#if (IndividualLocalAuth)
            services.AddDbContext<IdentityDbContext>(options =>
    #if (UseLocalDB)
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.MigrationsAssembly("Company.WebApplication1")
                ));
    #else
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.MigrationsAssembly("Company.WebApplication1")
                ));
    #endif
            services.AddIdentity<IdentityUser, IdentityRole>(options => options.Stores.MaxLengthForKeys = 128)
                .AddEntityFrameworkStores<IdentityDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

#elif (OrganizationalAuth || IndividualB2CAuth)
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
    #if (OrganizationalAuth)
                .AddAzureAd(options => Configuration.Bind("AzureAd", options))
    #elif (IndividualB2CAuth)
                .AddAzureAdB2C(options => Configuration.Bind("AzureAdB2C", options))
    #endif
            .AddCookie();

#endif
#if (IndividualLocalAuth)
            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/Account/Manage");
                    options.Conventions.AuthorizePage("/Account/Logout");
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

#elif (OrganizationalAuth)
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AllowAnonymousToFolder("/Account");
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
#else
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
#if (UseBrowserLink)
                app.UseBrowserLink();
#endif
                app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
                app.UseDatabaseErrorPage();
#endif
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();

#endif
            app.UseMvc();
        }
    }
}
