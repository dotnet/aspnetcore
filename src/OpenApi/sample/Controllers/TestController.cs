// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost]
    [Route("/forms")]
    public IActionResult PostForm([FromForm] MvcTodo todo)
    {
        return Ok(todo);
    }

    public class RouteParamsContainer
    {
        [FromRoute]
        public int Id { get; set; }

        [FromRoute]
        [MinLength(5)]
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "MinLengthAttribute works without reflection on string properties.")]
        public string? Name { get; set; }
    }

    public record MvcTodo(string Title, string Description, bool IsCompleted);
}
