// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class BanksController : Controller
{
    private readonly TestResponseGenerator _generator;

    public BanksController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("Banks/[action]/{id}")]
    [HttpGet("Bank/[action]/{id}")]
    public ActionResult Get(int id)
    {
        return _generator.Generate(
            Url.Action(),
            Url.RouteUrl(new { }));
    }

    [AcceptVerbs("PUT", Route = "Bank")]
    [HttpPatch("Bank")]
    [AcceptVerbs("PUT", Route = "Bank/Update")]
    [HttpPatch("Bank/Update")]
    public ActionResult UpdateBank()
    {
        return _generator.Generate(
            Url.Action(),
            Url.RouteUrl(new { }));
    }

    [AcceptVerbs("PUT", "POST")]
    [Route("Bank/Deposit")]
    [Route("Bank/Deposit/{amount}")]
    public ActionResult Deposit()
    {
        return _generator.Generate("/Bank/Deposit", "/Bank/Deposit/5");
    }

    [HttpPost]
    [Route("Bank/Withdraw/{id}")]
    public ActionResult Withdraw(int id)
    {
        return _generator.Generate("/Bank/Withdraw/5");
    }
}
