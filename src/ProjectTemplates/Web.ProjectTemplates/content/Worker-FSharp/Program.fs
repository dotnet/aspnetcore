namespace Company.Application1

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    let createHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(fun hostContext services ->
                services.AddHostedService<Worker>() |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args).Build().Run()

        0 // exit code