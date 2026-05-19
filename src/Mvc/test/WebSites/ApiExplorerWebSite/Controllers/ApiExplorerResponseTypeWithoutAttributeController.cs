// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

[Route("ApiExplorerResponseTypeWithoutAttribute/[Action]")]
public class ApiExplorerResponseTypeWithoutAttributeController : Controller
{
    [HttpGet]
    public void GetVoid()
    {
    }

    [HttpGet]
    public object GetObject()
    {
        return null;
    }

    [HttpGet]
    public IActionResult GetIActionResult()
    {
        return new EmptyResult();
    }

    [HttpGet]
    public ObjectResult GetDerivedActionResult()
    {
        return new ObjectResult(null);
    }

    [HttpGet]
    public Product GetProduct()
    {
        return null;
    }

    [HttpGet]
    public ActionResult<Product> GetActionResultProduct() => null;

    [HttpGet]
    public int GetInt()
    {
        return 0;
    }

    [HttpGet]
    public Task GetTask()
    {
        return Task.FromResult(true);
    }

    [HttpGet]
    public Task<object> GetTaskOfObject()
    {
        return Task.FromResult<object>(null);
    }

    [HttpGet]
    public Task<IActionResult> GetTaskOfIActionResult()
    {
        return Task.FromResult<IActionResult>(new EmptyResult());
    }

    [HttpGet]
    public Task<ObjectResult> GetTaskOfDerivedActionResult()
    {
        return Task.FromResult(new ObjectResult(null));
    }

    [HttpGet]
    public Task<Product> GetTaskOfProduct()
    {
        return Task.FromResult<Product>(null);
    }

    [HttpGet]
    public Task<int> GetTaskOfInt()
    {
        return Task.FromResult<int>(0);
    }
}
