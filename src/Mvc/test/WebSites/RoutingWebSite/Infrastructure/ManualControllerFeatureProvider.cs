// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Mvc.RoutingWebSite.Infrastructure;

internal class ManualControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly Action<ControllerFeature> _action;
    public ManualControllerFeatureProvider(Action<ControllerFeature> action)
    {
        _action = action;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        _action(feature);
    }
}

