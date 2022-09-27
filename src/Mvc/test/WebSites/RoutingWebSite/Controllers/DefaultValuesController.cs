// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class DefaultValuesController : Controller
{
    private readonly TestResponseGenerator _generator;

    public DefaultValuesController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult DefaultParameter(string id)
    {
        return _generator.Generate(id == null
            ? "/DefaultValuesRoute/DefaultValues"
            : "/DefaultValuesRoute/DefaultValues/DefaultParameter/Index/" + id);
    }

    public IActionResult OptionalParameter(string id)
    {
        return _generator.Generate(id == "17"
            ? "/DefaultValuesRoute/DefaultValues"
            : "/DefaultValuesRoute/DefaultValues/OptionalParameter/Index/" + id);
    }
}
