// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite
{
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
}
