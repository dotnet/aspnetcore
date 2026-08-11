// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The host contains no Blazor code. It uses WebApplication.CreateSlimBuilder so the Native AOT
// target stays lean while mirroring a real application. The root App component and every page live
// in the referenced BlazorServerAotSample.Pages class library.
//
// Components are described from *here* rather than from the library, because source generators all
// observe the same input compilation and cannot see each other's output: the Razor-generated types do
// not exist in the compilation of the project that declares the .razor files, but they do exist in its
// references.

using BlazorServerAotSample;
using BlazorServerAotSample.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if NATIVE_TESTING
using Microsoft.AspNetCore.Components.Testing.NativeAot.Generated;
#endif

#if NATIVE_TESTING
var builder = WebApplication.CreateSlimBuilder(args);
#else
var builder = WebApplication.CreateBuilder(args);
#endif

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5223");
}

AddInteractiveServerBlazor(builder.Services);
builder.Services.AddSingleton<IGreetingService, GreetingService>();
builder.Services.AddKeyedSingleton<IGreetingService, GreetingService>("aot-key");
builder.Services.AddSingleton<ProtectedBrowserStorageSerializer<Theme>, ThemeSerializer>();
AddSessionSupport(builder.Services);

#if NATIVE_TESTING
// CreateSlimBuilder does not execute hosting startups, so the harness the testing source generator
// baked into this compilation is registered explicitly. The symbol is defined only when the E2E
// harness is building this application, so a normal build has neither the code nor the call.
builder.Services.AddNativeTestHarness();
#endif

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

static void AddInteractiveServerBlazor(IServiceCollection services)
{
    services.AddComponentMetadata<SampleMetadata>();
    services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.RootComponents.RegisterForJavaScript<AotDynamicRoot>("aot-dynamic-root");
        });
}

static void AddSessionSupport(IServiceCollection services)
{
    services.AddDistributedMemoryCache();
    services.AddSession();
}