// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers;

[EnableCors("AllowAll")]
[Route("api/[controller]")]
public class PersonController : Controller
{
    // GET api/person
    [HttpGet]
    public IEnumerable<string> Get()
    {
        HttpContext.Response.Headers.Add("MyCustomHeader", "My custom value");
        return new string[] { "value1", "value2" };
    }

    // POST api/person
    [HttpPost]
    public async Task<string> Post()
    {
        using (var reader = new StreamReader(Request.Body))
        {
            var plainTextBodyContent = await reader.ReadToEndAsync();
            return $"You posted: {plainTextBodyContent}";
        }
    }

    [HttpGet("referrer")]
    public string GetReferer()
    {
        return $"The referrer is: {Request.Headers.Referer.ToString()}";
    }

    // PUT api/person
    [HttpPut]
    public Person Put([FromBody, Required] Person person)
    {
        return person;
    }

    // DELETE api/person
    [HttpDelete]
    public string Delete()
    {
        var result = new StringBuilder();
        foreach (var header in Request.Headers)
        {
            result.AppendLine(FormattableString.Invariant($"{header.Key}: {string.Join(",", header.Value.ToArray())}"));
        }
        return "REQUEST HEADERS:\n" + result.ToString();
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
