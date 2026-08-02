// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(GroupName = "xml")]
public class XmlController : ControllerBase
{
    /// <summary>
    /// A summary of the action.
    /// </summary>
    /// <description>
    /// A description of the action.
    /// </description>
    [HttpGet]
    public string Get()
    {
        return "Hello, World!";
    }

    /// <param name="name">The name of the person.</param>
    /// <response code="200">Returns the greeting.</response>
    [HttpGet]
    public string Get1(string name)
    {
        return $"Hello, {name}!";
    }

    /// <param name="todo">The todo to insert into the database.</param>
    [HttpPost]
    public string Post(Todo todo)
    {
        return $"Hello, {todo.Title}!";
    }

    public record Todo(int Id, string Title, bool Completed);
}
