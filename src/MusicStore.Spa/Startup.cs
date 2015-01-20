using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Security;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using MusicStore.Spa.Infrastructure;

namespace MusicStore.Spa
{
    public class Startup
    {
        public Startup()
        {
            Configuration = new Configuration()
                        .AddJsonFile("Config.json")
                        .AddEnvironmentVariables();
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SiteSettings>(settings =>
            {
                settings.DefaultAdminUsername = Configuration.Get("DefaultAdminUsername");
                settings.DefaultAdminPassword = Configuration.Get("DefaultAdminPassword");
            });

            // Add MVC services to the service container
            services.AddMvc();

            services.Configure<MvcOptions>(options =>
            {
                options.ModelValidatorProviders.Add(typeof(BuddyValidatorProvider));
            });

            // Add EF services to the service container
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<MusicStoreContext>(options =>
                {
                    options.UseSqlServer(Configuration.Get("Data:DefaultConnection:ConnectionString"));
                });

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>(Configuration)
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders()
                    .AddMessageProvider<EmailMessageProvider>()
                    .AddMessageProvider<SmsMessageProvider>();

            // Add application services to the service container
            //services.AddTransient<IModelMetadataProvider, BuddyModelMetadataProvider>();

            // Configure Auth
            services.Configure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("app-ManageStore", new AuthorizationPolicyBuilder().RequiresClaim("app-ManageStore", "Allowed").Build());
            });

        }

        public void Configure(IApplicationBuilder app)
        {
            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();

            // Configure the HTTP request pipeline

            // Add cookie auth
            app.UseIdentity();

            // Add static files
            app.UseStaticFiles();

            // Add MVC
            app.UseMvc();
        }
    }
}
