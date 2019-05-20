namespace Company.WebApplication1.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

[<Route("api/SampleData/[controller]")>]
[<ApiController>]
type WeatherController () =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Get(location:string, unit:TemperatureUnit) =
        let rnd = System.Random()
        let result = new WeatherResult (Location = location, Temperature = rnd.Next(-20, 55), TemperatureUnit = unit)
        ActionResult<WeatherResult>(result)

    [<HttpGet>]
    member this.Get(location:string) =
        let rnd = System.Random()
        let result = new WeatherResult (Location = location, Temperature = rnd.Next(-20, 55), TemperatureUnit = TemperatureUnit.Celsius)
        ActionResult<WeatherResult>(result)

type TemperatureUnit =
   | Celsius
   | Fahrenheit

type WeatherResult =
    member val Temperature  "" with get, set
    member val TemperatureUnit "" with get, set
    member val Location  "" with get, set
