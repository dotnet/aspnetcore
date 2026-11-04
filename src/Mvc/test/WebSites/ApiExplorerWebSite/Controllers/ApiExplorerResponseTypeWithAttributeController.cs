// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("[controller]/[Action]")]
public class ApiExplorerResponseTypeWithAttributeController : Controller
{
    [HttpGet]
    [ProducesResponseType(typeof(void), 204)]
    public void GetVoidWithExplicitResponseTypeStatusCode()
    {
    }

    [HttpGet]
    [Produces(typeof(Customer))]
    public void GetVoid()
    {
    }

    [HttpGet]
    [Produces("application/json", Type = typeof(Product))]
    public object GetObject()
    {
        return null;
    }

    [HttpGet]
    [Produces("application/json", Type = typeof(string))]
    public IActionResult GetIActionResult()
    {
        return new EmptyResult();
    }

    [HttpGet]
    [ProducesResponseType(typeof(void), 204)]
    public Task GetTaskWithExplicitResponseTypeStatusCode()
    {
        return Task.FromResult(true);
    }

    [HttpGet]
    [Produces("application/json", Type = typeof(int))]
    public Task GetTask()
    {
        return null;
    }

    [HttpGet]
    [Produces(typeof(Customer))] // It's possible to lie about what type you return
    public Product GetProduct()
    {
        return null;
    }

    [ProducesResponseType(typeof(Product), 201)]
    [ProducesResponseType(typeof(SerializableError), 400)]
    public Product CreateProductWithDefaultResponseContentTypes(Product product)
    {
        return null;
    }

    [Produces("text/xml")] // Has status code as 200 but is not applied as it does not set 'Type'
    [ProducesResponseType(typeof(Product), 201)]
    [ProducesResponseType(typeof(SerializableError), 400)]
    public Product CreateProductWithLimitedResponseContentTypes(Product product)
    {
        return null;
    }

    [ProducesResponseType(typeof(Product), 200)]
    [ProducesResponseType(typeof(SerializableError), 400)]
    public Product UpdateProductWithDefaultResponseContentTypes(Product product)
    {
        return null;
    }

    [Produces("text/xml", Type = typeof(Product))] // Has status code as 200
    [ProducesResponseType(typeof(SerializableError), 400)]
    public Product UpdateProductWithLimitedResponseContentTypes(Product product)
    {
        return null;
    }
}
