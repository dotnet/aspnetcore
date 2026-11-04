// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class NonParameterConstraintController : Controller
{
    private readonly TestResponseGenerator _generator;

    public NonParameterConstraintController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate("/NonParameterConstraintRoute/NonParameterConstraint/Index");
    }
}
