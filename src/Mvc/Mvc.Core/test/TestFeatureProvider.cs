// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Mvc;

public class TestFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly Func<TypeInfo, bool> _filter;

    public TestFeatureProvider()
        : this(t => true)
    {
    }

    public TestFeatureProvider(Func<TypeInfo, bool> filter)
    {
        _filter = filter;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(t => t.Types).Where(_filter))
        {
            feature.Controllers.Add(type);
        }
    }
}
