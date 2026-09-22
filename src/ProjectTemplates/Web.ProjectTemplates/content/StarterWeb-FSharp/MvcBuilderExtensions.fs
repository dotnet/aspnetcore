namespace Company.WebApplication1

open System
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.Loader
open Microsoft.AspNetCore.Mvc.ApplicationParts
open Microsoft.Extensions.DependencyInjection

[<Extension>]
type MvcBuilderExtensions =
    [<Extension>]
    static member AddCompiledRazorViews(builder : IMvcBuilder) =
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
