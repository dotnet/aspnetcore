using IdentitySample.Models;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
#if DNX451
using NLog.Config;
using NLog.Targets;
#endif

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
            services.AddEntityFramework()
                    .AddSqlServer()
                    .AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.Get("Data:IdentityConnection:ConnectionString")));
            services.Configure<IdentityDbContextOptions>(options =>
            {
                options.DefaultAdminUserName = Configuration.Get("DefaultAdminUsername");
                options.DefaultAdminPassword = Configuration.Get("DefaultAdminPassword");
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

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

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
#if DNX451
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            consoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${ndc} ${logger} ${message} ";
            var rule1 = new LoggingRule("*", NLog.LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            loggerFactory.AddNLog(new global::NLog.LogFactory(config));
#endif
            app.UseErrorPage(ErrorPageOptions.ShowAll)
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