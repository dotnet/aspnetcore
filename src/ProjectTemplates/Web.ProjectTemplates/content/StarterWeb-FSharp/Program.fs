namespace Company.WebApplication1

#nowarn "20"

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Runtime.Loader
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
#if (!NoHttps)
open Microsoft.AspNetCore.HttpsPolicy
#endif
open Microsoft.AspNetCore.Mvc.ApplicationParts
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    let exitCode = 0

    // Razor source generation requires C#, so load the build-time compiled views explicitly.
    let private addCompiledRazorViews (builder : IMvcBuilder) =
        let applicationAssembly = Assembly.GetExecutingAssembly()
        let viewsAssemblyName = $"{applicationAssembly.GetName().Name}.Views"
        let viewsAssemblyPath = Path.Combine(AppContext.BaseDirectory, $"{viewsAssemblyName}.dll")
        let loadContext = AssemblyLoadContext.GetLoadContext(applicationAssembly)

        let viewsAssembly =
            if File.Exists(viewsAssemblyPath) then
                loadContext.LoadFromAssemblyPath(viewsAssemblyPath)
            else
                loadContext.LoadFromAssemblyName(AssemblyName(viewsAssemblyName))

        builder.PartManager.ApplicationParts.Add(CompiledRazorAssemblyPart(viewsAssembly))
        builder

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)

        builder
            .Services
            .AddControllersWithViews()
        |> addCompiledRazorViews

        builder.Services.AddRazorPages()

        let app = builder.Build()

        if not (builder.Environment.IsDevelopment()) then
#if (HasHttpsProfile)
            app.UseExceptionHandler("/Home/Error")
            app.UseHsts() |> ignore // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.

        app.UseHttpsRedirection()
#else
            app.UseExceptionHandler("/Home/Error") |> ignore
#endif

        app.UseStaticFiles()
        app.UseRouting()
        app.UseAuthorization()

        app.MapControllerRoute(name = "default", pattern = "{controller=Home}/{action=Index}/{id?}")

        app.MapRazorPages()

        app.Run()

        exitCode
