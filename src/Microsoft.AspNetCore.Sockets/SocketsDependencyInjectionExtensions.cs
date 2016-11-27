using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SocketsDependencyInjectionExtensions
    {
        public static IServiceCollection AddSockets(this IServiceCollection services)
        {
            services.AddRouting();
            services.TryAddSingleton<ConnectionManager>();
            services.TryAddSingleton<PipelineFactory>();
            services.TryAddSingleton<HttpConnectionDispatcher>();
            return services;
        }
    }
}
