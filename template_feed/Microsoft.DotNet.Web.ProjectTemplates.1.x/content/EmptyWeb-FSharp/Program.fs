namespace Company.WebApplication1

open System
open System.IO
open Microsoft.AspNetCore.Hosting

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        let host = 
            WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
    #if (IncludeApplicationInsights)
                .UseApplicationInsights()
    #endif
                .Build()

        host.Run()

        exitCode