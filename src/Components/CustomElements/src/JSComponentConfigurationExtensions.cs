// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Extension methods for registering custom elements from an <see cref="IJSComponentConfiguration"/>.
/// </summary>
public static class CustomElementsJSComponentConfigurationExtensions
{
    /// <summary>
    /// Marks the specified component type as allowed for use as a custom element.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
    /// <param name="identifier">A unique name for the custom element. This must conform to custom element naming rules, so it must contain a dash character.</param>
    public static void RegisterCustomElement<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(this IJSComponentConfiguration configuration, string identifier) where TComponent : IComponent
        => configuration.RegisterForJavaScript<TComponent>(identifier, "registerBlazorCustomElement");
}
