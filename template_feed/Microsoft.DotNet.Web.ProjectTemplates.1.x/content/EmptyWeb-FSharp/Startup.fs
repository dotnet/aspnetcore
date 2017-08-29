namespace Company.WebApplication1

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        ()

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment, log: ILoggerFactory) =
        log.AddConsole() |> ignore

        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        app.Run(fun context -> context.Response.WriteAsync("Hello World!"))

        ()