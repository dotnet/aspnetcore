using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
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
        public string? Name { get; set; }
    }

    public record MvcTodo(string Title, string Description, bool IsCompleted);
}
