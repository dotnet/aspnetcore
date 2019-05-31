namespace WebApplication1.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc

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
type WeatherController () =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Get(location:string, unit: string) =
        let rnd = System.Random()
        let result:WeatherResult = {
            Location = location;
            Temperature = rnd.Next(-20,55);
            TemperatureUnit = unit
        }
        ActionResult<WeatherResult>(result)