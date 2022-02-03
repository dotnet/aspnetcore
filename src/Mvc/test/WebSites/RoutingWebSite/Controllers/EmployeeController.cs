// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// This controller combines routes on the controller with routes on actions in a REST + navigation property
// style.
[Route("api/Employee")]
public class EmployeeController : Controller
{
    private readonly TestResponseGenerator _generator;

    public EmployeeController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult List()
    {
        return _generator.Generate("/api/Employee");
    }

    [AcceptVerbs("PUT", "PATCH")]
    public IActionResult UpdateEmployee()
    {
        return _generator.Generate("/api/Employee");
    }

    [AcceptVerbs("PUT", "PATCH", Route = "Manager")]
    public IActionResult UpdateManager()
    {
        return _generator.Generate("/api/Employee/Manager");
    }

    [HttpMerge("{id}")]
    public IActionResult MergeEmployee(int id)
    {
        return _generator.Generate("/api/Employee/" + id);
    }

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        return _generator.Generate("/api/Employee/" + id);
    }

    [HttpGet("{id}/Administrator")]
    public IActionResult GetAdministrator(int id)
    {
        return _generator.Generate("/api/Employee/" + id + "/Administrator");
    }

    [HttpGet("~/Manager/{id}")]
    public IActionResult GetManager(int id)
    {
        return _generator.Generate("/Manager/" + id);
    }

    [HttpDelete("{id}/Administrator")]
    public IActionResult DeleteAdministrator(int id)
    {
        return _generator.Generate("/api/Employee/" + id + "/Administrator");
    }

    [Route("{id}/Salary")]
    public IActionResult Salary(int id)
    {
        return _generator.Generate("/api/Employee/" + id + "/Salary");
    }
}
