#if (UseWebAssembly)
using BlazorWeb_CSharp.Client.Pages;
#endif
using BlazorWeb_CSharp.Components;

namespace BlazorWeb_CSharp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        #if (!UseServer && !UseWebAssembly)
        builder.Services.AddRazorComponents();
        #else
        builder.Services.AddRazorComponents()
          #if (UseServer && UseWebAssembly)
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
          #elif(UseServer)
            .AddInteractiveServerComponents();
          #elif(UseWebAssembly)
            .AddInteractiveWebAssemblyComponents();
          #endif
        #endif

        var app = builder.Build();

        // Configure the HTTP request pipeline.
#if (UseWebAssembly)
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
#else
        if (!app.Environment.IsDevelopment())
#endif
        {
            app.UseExceptionHandler("/Error");
        #if (HasHttpsProfile)
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        #endif
        }

        #if (HasHttpsProfile)
        app.UseHttpsRedirection();

        #endif
        app.UseStaticFiles();
        app.UseAntiforgery();

        #if (UseServer && UseWebAssembly)
        app.MapRazorComponents<App>()
          .AddInteractiveServerRenderMode()
          .AddInteractiveWebAssemblyRenderMode()
          .AddAdditionalAssemblies(typeof(Counter).Assembly);
        #elif (UseServer)
        app.MapRazorComponents<App>()
          .AddInteractiveServerRenderMode();
        #elif (UseWebAssembly)
        app.MapRazorComponents<App>()
          .AddInteractiveWebAssemblyRenderMode()
          .AddAdditionalAssemblies(typeof(Counter).Assembly);
        #else
        app.MapRazorComponents<App>();
        #endif

        app.Run();
    }
}
