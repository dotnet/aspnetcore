using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.Hubs;
using SocketsSample.Protobuf;

namespace SocketsSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddSignalR();
                    // .AddRedis();

            services.AddSingleton<ChatEndPoint>();
            services.AddSingleton<ProtobufSerializer>();
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

            app.UseSignalR(routes =>
            {
                routes.MapHub<Chat>("/hubs");
            });

            app.UseSockets(routes =>
            {
                routes.MapSocketEndpoint<ChatEndPoint>("/chat");
                routes.MapSocketEndpoint<RpcEndpoint<Echo>>("/jsonrpc");
            });

            app.UseRpc(invocationAdapters =>
            {
                invocationAdapters.AddInvocationAdapter("protobuf", new ProtobufInvocationAdapter(app.ApplicationServices));
                invocationAdapters.AddInvocationAdapter("json", new JsonInvocationAdapter());
                invocationAdapters.AddInvocationAdapter("line", new LineInvocationAdapter());
            });
        }
    }
}
