// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("ApiExplorerWithTypedResult/[Action]")]
public class ApiExplorerWithTypedResultController : Controller
{
    [HttpGet]
    public Ok<Product> GetProduct() => TypedResults.Ok(new Product { Name = "Test product" });
}
