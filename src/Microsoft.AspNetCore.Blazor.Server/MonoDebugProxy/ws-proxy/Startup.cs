using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using System.Net.WebSockets;
using System.Threading;
using System.IO;
using System.Text;

namespace WsProxy
{

	internal class Startup {
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices (IServiceCollection services)
		{
			services.AddRouting ();
		}

		public static async Task ProxyMsg (string desc, WebSocket from, WebSocket to)
		{
			byte [] buff = new byte [4000];
			var mem = new MemoryStream ();
			while (true) {
				var result = await from.ReceiveAsync (new ArraySegment<byte> (buff), CancellationToken.None);
				if (result.MessageType == WebSocketMessageType.Close) {
					await to.SendAsync (new ArraySegment<byte> (mem.GetBuffer (), 0, (int)mem.Length), WebSocketMessageType.Close, true, CancellationToken.None);
					return;
				}

				if (result.EndOfMessage) {
					mem.Write (buff, 0, result.Count);

					var str = Encoding.UTF8.GetString (mem.GetBuffer (), 0, (int)mem.Length);

					await to.SendAsync (new ArraySegment<byte> (mem.GetBuffer (), 0, (int)mem.Length), WebSocketMessageType.Text, true, CancellationToken.None);
					mem.SetLength (0);
				} else {
					mem.Write (buff, 0, result.Count);
				}
			}
		}
		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /*
		public void Configure (IApplicationBuilder app, IHostingEnvironment env)
		{
			//loggerFactory.AddConsole();
			//loggerFactory.AddDebug();
			app.UseDeveloperExceptionPage ();

			app.UseWebSockets (); app.UseRouter (router => {
				router.MapGet ("devtools/page/{pageId}", async context => {
					if (!context.WebSockets.IsWebSocketRequest) {
						context.Response.StatusCode = 400;
						return;
					}

					try {
						var proxy = new MonoProxy ();
						await proxy.Run (context);
					} catch (Exception e) {
						Console.WriteLine ("got exception {0}", e);
					}
				});
			});
		}
        */
	}
}
