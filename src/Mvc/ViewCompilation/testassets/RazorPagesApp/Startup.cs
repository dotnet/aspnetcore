using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.LoginPath = "/Login");
            ConfigureMvc(builder);
        }

        public void Configure(IApplicationBuilder app, ILoggingBuilder builder)
        {
            builder.AddConsole();
            app.UseAuthentication();
            app.UseMvc();
        }

        public void ConfigureMvc(IMvcBuilder builder)
        {
            builder.AddRazorPagesOptions(options =>
            {
                options.RootDirectory = "/Pages";
                options.Conventions.AuthorizeFolder("/Auth");
            });
        }
    }
}
