// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite.Controllers;

public class ApiExplorerHttpResultsController : Controller
{
    [HttpGet("ApiExplorerHttpResultsController/GetOfT")]
    public Results<Ok<int>, NotFound<DateTime>> Get()
    {
        return Random.Shared.Next() % 2 == 0
            ? TypedResults.Ok(0)
            : TypedResults.NotFound(DateTime.Now);
    }

    [HttpGet("ApiExplorerHttpResultsController/GetWithNotFoundWithNoType")]
    public async Task<Results<Ok<int>, NotFound>> GetWithNotFoundWithNoType()
    {
        await Task.Delay(1);
        return Random.Shared.Next() % 2 == 0
            ? TypedResults.Ok(0)
            : TypedResults.NotFound();
    }
    [ProducesResponseType(200, Type = typeof(long))]
    [HttpGet("ApiExplorerHttpResultsController/GetWithDifferentProduceType")]
    public async Task<Results<Ok<int>, NotFound>> GetWithDifferentProduceType()
    {
        await Task.Delay(1);
        return Random.Shared.Next() % 2 == 0
            ? TypedResults.Ok(0)
            : TypedResults.NotFound();
    }
}
