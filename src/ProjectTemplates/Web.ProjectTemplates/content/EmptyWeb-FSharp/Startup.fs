namespace Company.WebApplication1

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        ()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseRouting() |> ignore

        app.UseEndpoints(fun endpoints ->
            endpoints.MapGet("/", fun context -> context.Response.WriteAsync("Hello World!")) |> ignore
            ) |> ignore
