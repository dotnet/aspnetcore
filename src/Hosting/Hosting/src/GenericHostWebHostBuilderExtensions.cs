using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    public static class GenericHostWebHostBuilderExtensions
    {
        public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
            var webhostBuilder = new GenericWebHostBuilder(builder);
            configure(webhostBuilder);
            builder.ConfigureServices((context, services) => services.AddHostedService<GenericWebHostService>());
            return builder;
        }
    }
}
