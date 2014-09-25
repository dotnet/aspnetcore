using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.Twitter;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using IdentitySample.Models;

namespace IdentitySamples
{
    public static class UseExt {

        /**
        * TODO: Middleware constructors need to take IOptionsAccessor<TOptions>

        * Move options setup into a different method?

        * Cookie options need to be different, named service/option instances? i.e.  Singleton Named Options

            SetupNamedOption("ApplicationCookie", options => { })
            UseCookieAuthentication("ApplicationCookie")
            SetupNamedOption("ExternalCookie", options => { })
            UseCookieAuthentication("ApplicationCookie")

            // Overloads which use default/no name
            SetupOption(options => { })
            UseGoogleAuthentication()

        */

        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GoogleAuthenticationMiddleware>();
            //return builder.UseGoogleAuthentication(b => 
            //    b.ApplicationServices.GetService<IOptionsAccessor<GoogleAuthenticationOptions>>().Options);
        }



        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder builder, Func<IApplicationBuilder, GoogleAuthenticationOptions> func)
        {
            return builder.UseGoogleAuthentication(func(builder));
        }

        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder builder)
        {
            // This should go inside of the middleware delegate
            return builder.UseFacebookAuthentication(b =>
                b.ApplicationServices.GetService<IOptionsAccessor<FacebookAuthenticationOptions>>().Options);
        }

        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder builder, Func<IApplicationBuilder, FacebookAuthenticationOptions> func)
        {
            return builder.UseFacebookAuthentication(func(builder));
        }

        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseTwitterAuthentication(b =>
                b.ApplicationServices.GetService<IOptionsAccessor<TwitterAuthenticationOptions>>().Options);
        }

        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder builder, Func<IApplicationBuilder, TwitterAuthenticationOptions> func)
        {
            return builder.UseTwitterAuthentication(func(builder));
        }
    }

    public partial class Startup()
    {
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

        public void Configure(IApplicationBuilder app)
        {
            app.UseServices(services =>
            {
                // Add EF services to the services container
                services.AddEntityFramework()
                        .AddSqlServer();

                // Configure DbContext           
                services.SetupOptions<IdentityDbContextOptions>(options =>
                {
                    options.DefaultAdminUserName = Configuration.Get("DefaultAdminUsername");
                    options.DefaultAdminPassword = Configuration.Get("DefaultAdminPassword");
                    options.UseSqlServer(Configuration.Get("Data:IdentityConnection:ConnectionString"));
                });

                // Add Identity services to the services container
                services.AddDefaultIdentity<ApplicationDbContext, ApplicationUser, IdentityRole>(Configuration);

                // move this into add identity along with the 
                //service.SetupOptions<ExternalAuthenticationOptions>(options => options.SignInAsAuthenticationType = "External")

                services.SetupOptions<IdentityOptions>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonLetterOrDigit = false;
                    options.SecurityStampValidationInterval = TimeSpan.Zero;
                });
                services.SetupOptions<GoogleAuthenticationOptions>(options =>
                {
                    options.ClientId = "514485782433-fr3ml6sq0imvhi8a7qir0nb46oumtgn9.apps.googleusercontent.com";
                    options.ClientSecret = "V2nDD9SkFbvLTqAUBWBBxYAL";
                });
                services.AddInstance(new GoogleAuthenticationOptions
                {
                    ClientId = "514485782433-fr3ml6sq0imvhi8a7qir0nb46oumtgn9.apps.googleusercontent.com",
                    ClientSecret = "V2nDD9SkFbvLTqAUBWBBxYAL"
                });
                services.SetupOptions<FacebookAuthenticationOptions>(options =>
                {
                    options.AppId = "901611409868059";
                    options.AppSecret = "4aa3c530297b1dcebc8860334b39668b";
                });

                services.SetupOptions<FacebookAuthenticationOptions>(options =>
                {
                    options.AppId = "901611409868059";
                    options.AppSecret = "4aa3c530297b1dcebc8860334b39668b";
                });

                services.SetupOptions<TwitterAuthenticationOptions>(options =>
                {
                    options.ConsumerKey = "BSdJJ0CrDuvEhpkchnukXZBUv";
                    options.ConsumerSecret = "xKUNuKhsRdHD03eLn67xhPAyE1wFFEndFo1X2UJaK2m1jdAxf4";
                });

                // Add MVC services to the services container
                services.AddMvc();
            });

            /* Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
             * Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
             */
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Setup identity cookie middleware
            // Add cookie-based authentication to the request pipeline
            app.UseIdentity();

            app.UseGoogleAuthentication();
            app.UseFacebookAuthentication();
            app.UseTwitterAuthentication();

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
            });

            //Populates the Admin user and role
            SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();
        }

        // TODO: Move services here
        public IServiceProvider ConfigureServices(ServiceCollection services)
        {
            return services.BuildServiceProvider();
        }

    }
}