// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Route("/Teams", Order = 1)]
public class TeamController : Controller
{
    private readonly TestResponseGenerator _generator;

    public TeamController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("/Team/{teamId}", Order = 2)]
    public ActionResult GetTeam(int teamId)
    {
        return _generator.Generate("/Team/" + teamId);
    }

    [HttpGet("/Team/{teamId}")]
    public ActionResult GetOrganization(int teamId)
    {
        return _generator.Generate("/Team/" + teamId);
    }

    [HttpGet("")]
    public ActionResult GetTeams()
    {
        return _generator.Generate("/Teams");
    }

    [HttpGet("", Order = 0)]
    public ActionResult GetOrganizations()
    {
        return _generator.Generate("/Teams");
    }

    [HttpGet("/Club/{clubId?}")]
    public ActionResult GetClub()
    {
        return Content(Url.Action(), "text/plain");
    }

    [HttpGet("/Organization/{clubId?}", Order = 1)]
    public ActionResult GetClub(int clubId)
    {
        return Content(Url.Action(), "text/plain");
    }

    [HttpGet("AllTeams")]
    public ActionResult GetAllTeams()
    {
        return Content(Url.Action(), "text/plain");
    }

    [HttpGet("AllOrganizations", Order = 0)]
    public ActionResult GetAllTeams(int notRelevant)
    {
        return Content(Url.Action(), "text/plain");
    }

    [HttpGet("/TeamName/{*Name=DefaultName}/")]
    public ActionResult GetTeam(string name)
    {
        return _generator.Generate("/TeamName/" + name);
    }
}
