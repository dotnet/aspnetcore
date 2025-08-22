// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(GroupName = "controllers")]
public class TestController : ControllerBase
{
    [HttpGet]
    [Route("/getbyidandname/{id}/{name}")]
    public string GetByIdAndName(RouteParamsContainer paramsContainer)
    {
        return paramsContainer.Id + "_" + paramsContainer.Name;
    }

    [HttpGet]
    [Route("/gettypedresult")]
    public Ok<MvcTodo> GetTypedResult()
    {
        return TypedResults.Ok(new MvcTodo("Title", "Description", true));
    }

    [HttpPost]
    [Route("/forms")]
    public IActionResult PostForm([FromForm] MvcTodo todo)
    {
        return Ok(todo);
    }

    [HttpGet]
    [Route("/getcultureinvariant")]
    public Ok<CurrentWeather> GetCurrentWeather()
    {
        return TypedResults.Ok(new CurrentWeather(1.0f));
    }

    [Route("/nohttpmethod")]
    public ActionResult<CurrentWeather> NoHttpMethod()
        => Ok(new CurrentWeather(-100));

    [HttpQuery]
    [Route("/query")]
    public ActionResult<CurrentWeather> HttpQueryMethod()
        => Ok(new CurrentWeather(0));

    [HttpFoo] // See https://github.com/dotnet/aspnetcore/issues/60914
    [Route("/unsupported")]
    public ActionResult<CurrentWeather> UnsupportedHttpMethod()
        => Ok(new CurrentWeather(100));

    public class HttpQuery() : HttpMethodAttribute(["QUERY"]);

    public class HttpFoo() : HttpMethodAttribute(["FOO"]);

    public class RouteParamsContainer
    {
        [FromRoute(Name = "id")]
        public int Id { get; set; }

        [FromRoute(Name = "name")]
        [MinLength(5)]
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "MinLengthAttribute works without reflection on string properties.")]
        public string? Name { get; set; }
    }

    public record MvcTodo(string Title, string Description, bool IsCompleted);

    public record CurrentWeather([property: Range(-100.5f, 100.5f)] float Temperature = 0.1f);
}
