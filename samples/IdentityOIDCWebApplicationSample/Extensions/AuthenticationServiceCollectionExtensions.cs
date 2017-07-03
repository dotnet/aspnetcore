using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.Extensions
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApplicationAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddOpenIdConnect()
                .AddCookie();
            return services;
        }

        public static IApplicationBuilder UseHttps(this IApplicationBuilder builder)
        {
            var configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();
            var port = configuration.GetValue<int?>("Https:Port", null);
            var rewriteOptions = new RewriteOptions();
            rewriteOptions.AddRedirectToHttps(StatusCodes.Status301MovedPermanently, port);
            builder.UseRewriter(rewriteOptions);
            return builder;
        }
    }
}
