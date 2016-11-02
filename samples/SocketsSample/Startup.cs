using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.EndPoints.Hubs;
using SocketsSample.Hubs;

namespace SocketsSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddSingleton<IPubSub, Bus>();
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(PubSubHubLifetimeManager<>));
            services.AddSingleton(typeof(HubEndPoint<>), typeof(HubEndPoint<>));
            services.AddSingleton(typeof(RpcEndpoint<>), typeof(RpcEndpoint<>));

            services.AddSingleton<ChatEndPoint>();
            services.AddSingleton<Chat>();

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
                routes.MapSocketEndpoint<HubEndPoint<Chat>>("/hubs");
                routes.MapSocketEndpoint<ChatEndPoint>("/chat");
                routes.MapSocketEndpoint<RpcEndpoint<Echo>>("/jsonrpc");
            });

            app.UseRpc(invocationAdapters =>
            {
                invocationAdapters.AddInvocationAdapter("protobuf", new Protobuf.ProtobufInvocationAdapter(app.ApplicationServices));
                invocationAdapters.AddInvocationAdapter("json", new JsonInvocationAdapter());
                invocationAdapters.AddInvocationAdapter("line", new LineInvocationAdapter());
            });
        }
    }
}
