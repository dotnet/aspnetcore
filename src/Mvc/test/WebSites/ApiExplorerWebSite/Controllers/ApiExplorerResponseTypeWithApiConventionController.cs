// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[ApiController]
[Route("ApiExplorerResponseTypeWithApiConventionController/[Action]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class ApiExplorerResponseTypeWithApiConventionController : Controller
{
    [HttpGet]
    public Product GetProduct(int id) => null;

    [HttpGet]
    public Task<ActionResult<Product>> GetTaskOfActionResultOfProduct(int id) => null;

    [HttpGet]
    public IEnumerable<Product> GetProducts() => null;

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(202)]
    [ProducesResponseType(403)]
    public IActionResult PostWithConventions() => null;

    [HttpPost]
    [Produces("application/json", "text/json")]
    public IActionResult PostWithProduces(Product p) => null;

    [HttpPost]
    public Task<IActionResult> PostTaskOfProduct(Product p) => null;

    [HttpPut]
    public Task<IActionResult> Put(string id, Product product) => null;

    [HttpDelete]
    public Task<IActionResult> DeleteProductAsync(object id) => null;

    [HttpPost]
    [ApiConventionMethod(typeof(CustomConventions), nameof(CustomConventions.CustomConventionMethod))]
    public Task<IActionResult> PostItem(Product p) => null;
}

public static class CustomConventions
{
    [ProducesResponseType(302)]
    [ProducesResponseType(409)]
    public static void CustomConventionMethod() { }
}
