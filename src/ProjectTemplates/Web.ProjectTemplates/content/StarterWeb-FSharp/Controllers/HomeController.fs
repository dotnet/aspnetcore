namespace Company.WebApplication1.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

type HomeController (logger : ILogger<HomeController>) =
    inherit Controller()

    member this.Index () =
        this.View()

    member this.Privacy () =
        this.View()

    member this.Error () =
        this.View();
