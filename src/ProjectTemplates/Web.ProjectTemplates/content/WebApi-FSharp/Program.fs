namespace Company.WebApplication1
#nowarn "20"

open Microsoft.AspNetCore.Builder
#if !NoHttps
open Microsoft.AspNetCore.HttpsPolicy
#endif
open Microsoft.Extensions.DependencyInjection

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()

        let app = builder.Build()

#if HasHttpsProfile
        app.UseHttpsRedirection()
#endif

        app.UseAuthorization()
        app.MapControllers()

        app.Run()

        exitCode
