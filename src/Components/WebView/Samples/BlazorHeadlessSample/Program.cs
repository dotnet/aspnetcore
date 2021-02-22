
using Microsoft.AspNetCore.Components.WebView.Hosting;

var builder = WebViewHostBuilder.CreateDefault();

var host = builder.Build();

var renderClient = new ConsoleRenderClient();

host.AttachRenderClient(renderClient);

renderClient.AddComponent<App>("#app");

host.StartAsync()
