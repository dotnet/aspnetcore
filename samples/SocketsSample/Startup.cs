using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SocketsSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddSingleton<HubEndpoint>();
            services.AddSingleton<RpcEndpoint>();
            services.AddSingleton<ChatEndPoint>();

            services.AddSingleton<ProtobufSerializer>();
            services.AddSingleton<InvocationAdapterRegistry>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseFileServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseSockets(routes =>
            {
                routes.MapSocketEndpoint<HubEndpoint>("/hubs");
                routes.MapSocketEndpoint<ChatEndPoint>("/chat");
                routes.MapSocketEndpoint<RpcEndpoint>("/jsonrpc");
            });

            app.UseRpc(invocationAdapters =>
            {
                invocationAdapters.AddInvocationAdapter("protobuf", new Protobuf.ProtobufInvocationAdapter(app.ApplicationServices));
                invocationAdapters.AddInvocationAdapter("json", new JSonInvocationAdapter());
                invocationAdapters.AddInvocationAdapter("line", new LineInvocationAdapter());
            });
        }
    }
}
