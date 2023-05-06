namespace Company.WebApplication1
#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
#if (!NoHttps)
open Microsoft.AspNetCore.HttpsPolicy
#endif
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()

        let app = builder.Build()

#if (HasHttpsProfile)
        app.UseHttpsRedirection()
#endif

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
