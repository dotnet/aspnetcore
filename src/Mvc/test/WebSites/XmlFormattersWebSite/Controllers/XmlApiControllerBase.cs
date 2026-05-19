// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite;

[ApiController]
[Route("api/[controller]/[action]")]
public abstract class XmlApiControllerBase : ControllerBase
{
    [HttpGet]
    public ActionResult<Person> ActionReturningClientErrorStatusCodeResult()
        => NotFound();

    [HttpGet]
    public ActionResult<Person> ActionReturningProblemDetails()
    {
        return NotFound(new ProblemDetails
        {
            Instance = "instance",
            Title = "title",
            Extensions =
                {
                    ["Correlation"] = "correlation",
                    ["Accounts"] = new[] { "Account1", "Account2" },
                },
        });
    }

    [HttpGet]
    public ActionResult<Person> ActionReturningValidationProblem([FromQuery] Address address)
        => throw new NotImplementedException();

    [HttpGet]
    public ActionResult<Person> ActionReturningValidationDetailsWithMetadata()
    {
        return new BadRequestObjectResult(new ValidationProblemDetails
        {
            Detail = "some detail",
            Type = "some type",
            Extensions =
                {
                    ["CorrelationId"] = "correlation",
                },
            Errors =
                {
                    ["Error1"] = new[] { "ErrorValue"},
                },
        });
    }
}
