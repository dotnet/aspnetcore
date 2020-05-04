using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Certificate.Sample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate(options =>
                {
                });

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/required", context =>
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        return context.Response.WriteAsync($"Hello {context.User.Identity.Name} at {context.Request.Host}");
                    }
                    else
                    {
                        var request = context.Request;
                        var redirect = UriHelper.BuildAbsolute("https", new HostString("127.0.0.2", context.Connection.LocalPort), request.PathBase, request.Path, request.QueryString);
                        context.Response.Redirect(redirect, permanent: false, preserveMethod: true);
                        return Task.CompletedTask;
                    }
                });
                endpoints.Map("/signout", context =>
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        // Closing the connection doesn't reset Chrome's state, it still remembers which client cert was last used for which host until you close the browser.
                        // context.Response.Headers[HeaderNames.Connection] = "close";

                        // Sign out by switching back to the other host. This isn't a real sign-out because the browser has still cached the client certificate for this host.
                        // The only real way to clear that is to close the browser.
                        if (context.Request.Host.Host.Equals("127.0.0.2"))
                        {
                            var request = context.Request;
                            var redirect = UriHelper.BuildAbsolute("https", new HostString("127.0.0.1", context.Connection.LocalPort), request.PathBase);
                            context.Response.Redirect(redirect, permanent: false, preserveMethod: true);
                        }
                        return context.Response.WriteAsync($"Goodbye {context.User.Identity.Name} at {context.Request.Host}");
                    }
                    else
                    {
                        return context.Response.WriteAsync("Already signed out.");
                    }
                });
                endpoints.Map("{*url}", context =>
                {
                    return context.Response.WriteAsync($"Hello {context.User.Identity.Name} at {context.Request.Host}");
                });
            });
        }
    }
}
