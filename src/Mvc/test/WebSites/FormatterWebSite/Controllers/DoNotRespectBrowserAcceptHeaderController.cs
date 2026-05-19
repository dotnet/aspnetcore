// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

public class DoNotRespectBrowserAcceptHeaderController : Controller
{
    [HttpGet]
    public Employee EmployeeInfo()
    {
        return new Employee()
        {
            Id = 10,
            Name = "John"
        };
    }

    [HttpGet]
    [Produces("application/xml")]
    public Employee EmployeeInfoWithProduces()
    {
        return new Employee()
        {
            Id = 20,
            Name = "Mike"
        };
    }

    [HttpPost]
    public IActionResult CreateEmployee([FromBody] Employee employee)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return new ObjectResult(employee);
    }

    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
