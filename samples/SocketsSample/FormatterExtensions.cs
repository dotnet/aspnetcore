using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SocketsSample
{
    public static class RpcExtensions
    {
        public static IApplicationBuilder UseRpc(this IApplicationBuilder app, Action<RpcBuilder> registerAdapters)
        {
            var adapters = app.ApplicationServices.GetRequiredService<InvocationAdapterRegistry>();
            registerAdapters(new RpcBuilder(adapters));
            return app;
        }
    }

    public class RpcBuilder
    {
        private InvocationAdapterRegistry _invocationAdapters;

        public RpcBuilder(InvocationAdapterRegistry invocationAdapters)
        {
            _invocationAdapters = invocationAdapters;
        }

        public void AddInvocationAdapter(string format, IInvocationAdapter adapter)
        {
            _invocationAdapters.RegisterInvocationAdapter(format, adapter);
        }
    }
}
