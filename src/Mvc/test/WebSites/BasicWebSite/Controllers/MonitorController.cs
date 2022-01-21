// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace BasicWebSite;

public class MonitorController : Controller
{
    private readonly ActionDescriptorCreationCounter _counterService;

    public MonitorController(IEnumerable<IActionDescriptorProvider> providers)
    {
        _counterService = providers.OfType<ActionDescriptorCreationCounter>().Single();
    }

    public IActionResult CountActionDescriptorInvocations()
    {
        return Content(_counterService.CallCount.ToString(CultureInfo.InvariantCulture));
    }
}
