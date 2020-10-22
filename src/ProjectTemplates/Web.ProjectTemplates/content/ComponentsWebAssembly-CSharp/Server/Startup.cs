#if (OrganizationalAuth || IndividualB2CAuth || IndividualLocalAuth)
using Microsoft.AspNetCore.Authentication;
#endif
using Microsoft.AspNetCore.Builder;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
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

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            services.AddAuthentication()
                .AddIdentityServerJwt();
#endif
#if (OrganizationalAuth)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
#if (GenerateApiOrGraph)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
#if (GenerateApi)
                        .AddDownstreamWebApi("DownstreamApi", Configuration.GetSection("DownstreamApi"))
#endif
#if (GenerateGraph)
                        .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
#endif
                        .AddInMemoryTokenCaches();
#else
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
#endif
#elif (IndividualB2CAuth)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
#if (GenerateApi)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAdB2C"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
                        .AddDownstreamWebApi("DownstreamApi", Configuration.GetSection("DownstreamApi"))
                        .AddInMemoryTokenCaches();
#else
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAdB2C"));
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
                app.UseMigrationsEndPoint();
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
