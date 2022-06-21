// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Extension methods for working on an <see cref="IJSComponentConfiguration"/>.
/// </summary>
public static class JSComponentConfigurationExtensions
{
    // Having independent overloads for the cases with javaScriptInitializer and without it is needed for linkability,
    // since calling the underlying .Add method with javaScriptInitializer is what causes the linker to retain code for
    // the initializer feature.

    /// <summary>
    /// Marks the specified component type as allowed for instantiation from JavaScript.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
    /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
    public static void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(this IJSComponentConfiguration configuration, string identifier) where TComponent : IComponent
        => RegisterForJavaScript(configuration, typeof(TComponent), identifier);

    /// <summary>
    /// Marks the specified component type as allowed for instantiation from JavaScript.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
    /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
    /// <param name="javaScriptInitializer">Specifies an optional identifier for a JavaScript function that will be called to register the custom element.</param>
    public static void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(this IJSComponentConfiguration configuration, string identifier, string javaScriptInitializer) where TComponent : IComponent
        => RegisterForJavaScript(configuration, typeof(TComponent), identifier, javaScriptInitializer);

    /// <summary>
    /// Marks the specified component type as allowed for instantiation from JavaScript.
    /// </summary>
    /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(JSComponentInterop))]
    public static void RegisterForJavaScript(this IJSComponentConfiguration configuration, [DynamicallyAccessedMembers(Component)] Type componentType, string identifier)
        => configuration.JSComponents.Add(componentType, identifier);

    /// <summary>
    /// Marks the specified component type as allowed for instantiation from JavaScript.
    /// </summary>
    /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
    /// <param name="javaScriptInitializer">Specifies an optional identifier for a JavaScript function that will be called to register the custom element.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(JSComponentInterop))]
    public static void RegisterForJavaScript(this IJSComponentConfiguration configuration, [DynamicallyAccessedMembers(Component)] Type componentType, string identifier, string javaScriptInitializer)
        => configuration.JSComponents.Add(componentType, identifier, javaScriptInitializer);
}
