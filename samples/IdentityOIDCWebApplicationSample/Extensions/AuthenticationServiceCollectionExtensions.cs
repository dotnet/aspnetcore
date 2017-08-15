using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.Extensions
{
    public static class AuthenticationServiceCollectionExtensions
    {
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
