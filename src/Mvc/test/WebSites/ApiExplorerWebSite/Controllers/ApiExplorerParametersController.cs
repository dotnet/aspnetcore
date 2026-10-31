// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiExplorerWebSite.Controllers;

[Route("ApiExplorerParameters/[action]")]
public class ApiExplorerParametersController : Controller
{
    public void SimpleParameters(int i, string s)
    {
    }

    public void SimpleParametersWithBinderMetadata([FromQuery] int i, [FromRoute] string s)
    {
    }

    public void SimpleModel(Product product)
    {
    }

    [Route("{id}")]
    public void SimpleModelFromBody(int id, [FromBody] Product product)
    {
    }

    public void ComplexModel([FromQuery] OrderDTO order)
    {
    }

    public void DefaultValueParameters(string searchTerm, int top = 10, DayOfWeek searchDay = DayOfWeek.Wednesday)
    {
    }

    public void IsRequiredParameters([BindRequired] string requiredParam, string notRequiredParam, Product product)
    {
    }
}
