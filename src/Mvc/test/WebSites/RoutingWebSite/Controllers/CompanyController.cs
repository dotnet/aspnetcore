// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// A controller can define a route for all of the actions
// in it and give it a name for link generation purposes.
[Route("api/Company/{id}", Name = "Company")]
public class CompanyController : Controller
{
    private readonly TestResponseGenerator _generator;

    public CompanyController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    // An action with the same template will inherit the name
    // from the controller.
    [HttpGet]
    public ActionResult Get(int id)
    {
        return _generator.Generate(Url.RouteUrl("Company", new { id = id }));
    }

    // Multiple actions can have the same named route as long
    // as for a given Name, all the actions have the same template.
    // That is, there can't be two link generation entries with same
    // name and different templates.
    [HttpPut]
    public ActionResult Put(int id)
    {
        return _generator.Generate(Url.RouteUrl("Company", new { id = id }));
    }

    // Two actions can have the same template and each of them can have
    // a different route name. That is, a given template can have multiple
    // names associated with it.
    [HttpDelete(Name = "RemoveCompany")]
    public ActionResult Delete(int id)
    {
        return _generator.Generate(Url.RouteUrl("RemoveCompany", new { id = id }));
    }

    // An action that defines a non empty template doesn't inherit the name
    // from the route on the controller .
    [HttpGet("Employees")]
    public ActionResult GetEmployees(int id)
    {
        return _generator.Generate(Url.RouteUrl(new { id = id }));
    }

    // An action that defines a non empty template doesn't inherit the name
    // from the controller but can perfectly define its own name.
    [HttpGet("Departments", Name = "Departments")]
    public ActionResult GetDepartments(int id)
    {
        return _generator.Generate(Url.RouteUrl("Departments", new { id = id }));
    }
}
