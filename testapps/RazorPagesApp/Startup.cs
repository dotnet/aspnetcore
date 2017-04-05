using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RazorPagesApp
{
    public class Startup : IDesignTimeMvcBuilderConfiguration
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services.AddMvc();
            ConfigureMvc(builder);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                LoginPath = "/Login",
                AutomaticAuthenticate = true,
                AutomaticChallenge = true
            });

            app.UseMvc();
        }

        public void ConfigureMvc(IMvcBuilder builder)
        {
            builder.AddRazorPagesOptions(options =>
            {
                options.RootDirectory = "/Pages";
                options.AuthorizeFolder("/Auth");
            });
        }
    }
}
