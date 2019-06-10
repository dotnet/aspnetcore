namespace WebApplication1.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

type public TemperatureUnit =
   | Celsius=0
   | Fahrenheit=1

type WeatherResult = {
    Location: string
    TemperatureUnit: TemperatureUnit
    Temperature: int
}

[<Route("api/SampleData/[controller]")>]
[<ApiController>]
type WeatherController (_logger : ILogger<WeatherController>) =
    inherit ControllerBase()
    let mutable logger = _logger

    [<HttpGet>]
    member this.Get(location:string, unit: TemperatureUnit) =
        let rnd = System.Random()
        let result:WeatherResult = {
            Location = location;
            Temperature = rnd.Next(-20,55);
            TemperatureUnit = unit
        }
        ActionResult<WeatherResult>(result)
