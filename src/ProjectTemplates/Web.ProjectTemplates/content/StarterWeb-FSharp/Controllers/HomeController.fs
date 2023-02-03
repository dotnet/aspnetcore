namespace Company.WebApplication1.Controllers

open System.Diagnostics
open Microsoft.AspNetCore.Mvc
open Company.WebApplication1.Models

type HomeController () =
    inherit Controller()

    member this.Index () =
        this.View()

    member this.Privacy () =
        this.View()

    [<ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)>]
    member this.Error () =
        let reqId =
            if isNull Activity.Current then
                this.HttpContext.TraceIdentifier
            else
                Activity.Current.Id

        this.View({ RequestId = reqId })
