using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for configuring the IWebHostBuilder.
    /// </summary>
    public static class GenericHostBuilderExtensions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IWebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the <see cref="IWebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <param name="builder">The <see cref="IHostBuilder" /> instance to configure</param>
        /// <param name="configure">The configure callback</param>
        /// <returns>The <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder ConfigureWebHostDefaults(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
            return builder.ConfigureWebHost(webHostBuilder =>
            {
                WebHost.ConfigureWebDefaults(webHostBuilder);

                configure(webHostBuilder);
            });
        }
    }
}
