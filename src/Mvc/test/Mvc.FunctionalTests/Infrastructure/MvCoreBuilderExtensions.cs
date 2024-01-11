// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

internal static class MvCoreBuilderExtensions
{
    internal static void UseSpecificControllers(
        this ApplicationPartManager partManager,
        params Type[] controllerTypes)
    {
        partManager.FeatureProviders.Add(new TestControllerFeatureProvider());
        partManager.ApplicationParts.Clear();
        partManager.ApplicationParts.Add(new SelectedControllersApplicationParts(controllerTypes));
    }

    internal static IMvcCoreBuilder UseSpecificControllers(
        this IMvcCoreBuilder mvcCoreBuilder,
        params Type[] controllerTypes) => mvcCoreBuilder
        .ConfigureApplicationPartManager(partManager => partManager.UseSpecificControllers(controllerTypes));
}

internal class SelectedControllersApplicationParts(Type[] types) : ApplicationPart, IApplicationPartTypeProvider
{
    public override string Name { get; } = string.Empty;

    public IEnumerable<TypeInfo> Types { get; } = types.Select(x => x.GetTypeInfo()).ToArray();
}

internal class TestControllerFeatureProvider : ControllerFeatureProvider
{
    // Default controller feature provider doesn't support nested controller classes
    // so we override that here
    protected override bool IsController(TypeInfo typeInfo) => true;
}
