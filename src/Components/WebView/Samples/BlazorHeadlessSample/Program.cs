using Microsoft.AspNetCore.Components.WebView.Headless;
using Microsoft.AspNetCore.Components.WebView.Hosting;

var builder = WebViewHostBuilder.CreateDefault();

var host = builder.Build();

var renderPort = new ConsoleWindow();

host.AttachRenderClient(renderPort);

renderPort.AddComponent<BlazorServerApp.App>("#app");
