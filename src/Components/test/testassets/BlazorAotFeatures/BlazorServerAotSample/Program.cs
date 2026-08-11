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

using System.Diagnostics.CodeAnalysis;
using BlazorServerAotSample;
using BlazorServerAotSample.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if E2E_COMPILE_TEST_HARNESS
using Microsoft.AspNetCore.Components.Testing.NativeAot.Generated;
#endif

var builder = WebApplication.CreateSlimBuilder(args);

// CreateSlimBuilder wires Kestrel core, the sockets transport and routing, but - unlike the non-slim
// builder - it does not load the static web assets manifest. Blazor Server serves
// _framework/blazor.web.js as a static web asset, so the manifest is loaded explicitly.
builder.WebHost.UseStaticWebAssets();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5223");
}

AddInteractiveServerBlazor(builder.Services);
builder.Services.AddSingleton<IGreetingService, GreetingService>();
builder.Services.AddKeyedSingleton<IGreetingService, GreetingService>("aot-key");
builder.Services.AddSingleton<ProtectedBrowserStorageSerializer<Theme>, ThemeSerializer>();
AddSessionSupport(builder.Services);

#if E2E_COMPILE_TEST_HARNESS
// CreateSlimBuilder does not execute hosting startups, so register the shared source-generated
// Native AOT harness explicitly. The extension is emitted only for the nested E2E publish.
builder.Services.AddE2ETestHarness();
#endif

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

// AddRazorComponents and AddInteractiveServerComponents keep their [RequiresUnreferencedCode] gates:
// they correctly warn an application that has not described its components. This one has, so the
// warning is suppressed at the call site rather than by de-annotating the framework.
[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "Every path this application exercises is covered by the descriptors in " +
        "SampleMetadata, which replace the reflection fallbacks these gates warn about.")]
static void AddInteractiveServerBlazor(IServiceCollection services)
{
    services.AddComponentMetadata<SampleMetadata>();
    services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.RootComponents.RegisterForJavaScript<AotDynamicRoot>("aot-dynamic-root");
        });
}

[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "This sample only stores generated-contract values through the session-backed component state feature.")]
static void AddSessionSupport(IServiceCollection services)
{
    services.AddDistributedMemoryCache();
    services.AddSession();
}