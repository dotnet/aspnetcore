#if (OrganizationalAuth || IndividualB2CAuth || IndividualLocalAuth)
using Microsoft.AspNetCore.Authentication;
#endif
using Microsoft.AspNetCore.Builder;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
#endif
#if (RequiresHttps)
using Microsoft.AspNetCore.HttpsPolicy;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
#if (IndividualLocalAuth)
using ComponentsWebAssembly_CSharp.Server.Data;
using ComponentsWebAssembly_CSharp.Server.Models;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif

namespace ComponentsWebAssembly_CSharp.Server
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

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            services.AddAuthentication()
                .AddIdentityServerJwt();
#endif
#if (OrganizationalAuth)
#if (GenerateApiOrGraph)
            // Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddMicrosoftWebApiAuthentication(Configuration, "AzureAd")
                .AddMicrosoftWebApiCallsWebApi(Configuration, "AzureAd")
                .AddInMemoryTokenCaches();
#else
            // Adds Microsoft Identity platform (AAD v2.0) support to protect this Api
            services.AddMicrosoftWebApiAuthentication(Configuration, "AzureAd");
#endif
#if (GenerateApi)
            services.AddDownstreamWebApiService(Configuration);
#endif
#if (GenerateGraph)
            services.AddMicrosoftGraph(Configuration.GetValue<string>("CalledApi:CalledApiScopes")?.Split(' '),
                                       Configuration.GetValue<string>("CalledApi:CalledApiUrl"));
#endif
#elif (IndividualB2CAuth)
#if (GenerateApi)
            services.AddMicrosoftWebApiAuthentication(Configuration, "AzureAdB2C")
                .AddMicrosoftWebApiCallsWebApi(Configuration, "AzureAdB2C")
                .AddInMemoryTokenCaches();

            services.AddDownstreamWebApiService(Configuration);
#else
            services.AddMicrosoftWebApiAuthentication(Configuration, "AzureAdB2C");
#endif
#endif

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
                app.UseDatabaseErrorPage();
#endif
                app.UseWebAssemblyDebugging();
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
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

#if (IndividualLocalAuth)
            app.UseIdentityServer();
#endif
#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();
#endif
#if (!NoAuth)
            app.UseAuthorization();

#endif
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
