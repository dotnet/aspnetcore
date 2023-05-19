
namespace Components_CSharp;

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
            .AddServerComponents()
            .AddWebAssemblyComponents();
          #elif(UseServer)
            .AddServerComponents();
          #elif(UseWebAssembly)
            .AddWebAssemblyComponents();
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

        app.MapRazorComponents<App>();

        app.Run();
    }
}
