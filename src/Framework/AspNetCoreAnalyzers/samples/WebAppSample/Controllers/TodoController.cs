// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Namespaces
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endregion

[Route("api/[controller]")]
public class TodoController
{
    private readonly DbContext _dbContext;

    public TodoController(DbContext dbContext) => _dbContext = dbContext;

    [HttpGet("{id}")]
    public Todo Get(int id) => _dbContext.Todos.Find(id);

    [HttpPut]
    public void Create([FromBody] Todo todo) => _dbContext.Todos.Add(todo);

    [HttpGet("[action]/{page:int?}")]
    public IEnumerable<Todo> Search(int? page, [FromQuery] string text)
    {
        return _dbContext.Todos
            .Where(t => t.Text.Contains(text))
            .Skip((page ?? 0) * 10)
            .Take(10);
    }
}
