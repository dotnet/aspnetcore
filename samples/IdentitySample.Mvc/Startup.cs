using IdentitySample.Models;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using System;

namespace IdentitySamples
{
    public partial class Startup
    {
        public Startup()
        {
            /* 
            * Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            * then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            */
            Configuration = new Configuration()
                .AddJsonFile("LocalConfig.json")
                .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
        }

        public IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFramework().AddSqlServer();
            services.AddScoped<ApplicationDbContext>();
            services.ConfigureOptions<IdentityDbContextOptions>(options =>
            {
                options.DefaultAdminUserName = Configuration.Get("DefaultAdminUsername");
                options.DefaultAdminPassword = Configuration.Get("DefaultAdminPassword");
                options.UseSqlServer(Configuration.Get("Data:IdentityConnection:ConnectionString"));
            });

            services.AddDefaultIdentity<ApplicationDbContext, ApplicationUser, IdentityRole>(Configuration, options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.SecurityStampValidationInterval = TimeSpan.FromMinutes(20);
            });

            services.ConfigureFacebookAuthentication(options =>
            {
                options.AppId = "901611409868059";
                options.AppSecret = "4aa3c530297b1dcebc8860334b39668b";
            });
            services.ConfigureGoogleAuthentication(options =>
            {
                options.ClientId = "514485782433-fr3ml6sq0imvhi8a7qir0nb46oumtgn9.apps.googleusercontent.com";
                options.ClientSecret = "V2nDD9SkFbvLTqAUBWBBxYAL";
            });
            services.ConfigureTwitterAuthentication(options =>
            {
                options.ConsumerKey = "BSdJJ0CrDuvEhpkchnukXZBUv";
                options.ConsumerSecret = "xKUNuKhsRdHD03eLn67xhPAyE1wFFEndFo1X2UJaK2m1jdAxf4";
            });
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseErrorPage(ErrorPageOptions.ShowAll)
               .UseServices()
               .UseStaticFiles()
               .UseIdentity()
               .UseFacebookAuthentication()
               .UseGoogleAuthentication()
               .UseTwitterAuthentication()
               .UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller}/{action}/{id?}",
                        defaults: new { controller = "Home", action = "Index" });
                });

            //Populates the Admin user and role 
            SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();
        }
    }
}