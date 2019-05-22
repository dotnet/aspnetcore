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
    let mutable _temperature : int = 0;
    let mutable _temperatureUnit : TemperatureUnit = null;
    let mutable _location : string = null;

    member x.Temperature
        with public get() : int = _temperature
        and  public set(value) = _temperature <- value

    member x.TemperatureUnit
        with public get() : TemperatureUnit = _temperatureUnit
        and  public set(value) = _temperatureUnit <- value

    member x.Location
        with public get() : string = _location
        and  public set(value) = _location <- value
