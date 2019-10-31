using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CustomPolicyProvider
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Replace the default authorization policy provider with our own
            // custom provider which can return authorization policies for given
            // policy names (instead of using the default policy provider)
            services.AddSingleton<IAuthorizationPolicyProvider, MinimumPolicyProvider>();

            // As always, handlers must be provided for the requirements of the authorization policies
            services.AddSingleton<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();
            services.AddSingleton<IAuthorizationHandler, MinimumValueAuthorizationHandler>();

            services.AddMvc();

            services.Configure<AuthorizationMiddlewareOptions>(options => options.AllowRequestContextInHandlerContext = true);

            // Add cookie authentication so that it's possible to sign-in to test the
            // custom authorization policy behavior of the sample
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.AccessDeniedPath = "/account/denied";
                    options.LoginPath = "/account/signin";
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/authorized-value/{value:int}", async context =>
                {
                    await context.Response.WriteAsync("Total is acceptable.");
                }).RequireAuthorization(MinimumPolicyProvider.MINIMUMVALUE_POLICY_PREFIX + "27");

                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
