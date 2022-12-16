using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if (Hosted)
using EmptyComponentsWebAssembly_CSharp.Client;
#else
using EmptyComponentsWebAssembly_CSharp;
#endif

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if (!Hosted)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
#else
builder.Services.AddHttpClient("EmptyComponentsWebAssembly_CSharp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("EmptyComponentsWebAssembly_CSharp.ServerAPI"));
#endif

await builder.Build().RunAsync();
