namespace Company.WebApplication1

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
#if (!NoHttps)
open Microsoft.AspNetCore.HttpsPolicy;
#endif
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type Startup(configuration: IConfiguration) =
    member _.Configuration = configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member _.ConfigureServices(services: IServiceCollection) =
        // Add framework services.
        services.AddControllers() |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
#if (!NoHttps)
        app.UseHttpsRedirection()
           .UseRouting()
           .UseAuthorization()
           .UseEndpoints(fun endpoints ->
                endpoints.MapControllers() |> ignore
            ) |> ignore
#else
        app.UseRouting()
           .UseAuthorization()
           .UseEndpoints(fun endpoints ->
                 endpoints.MapControllers() |> ignore
             ) |> ignore
#endif
