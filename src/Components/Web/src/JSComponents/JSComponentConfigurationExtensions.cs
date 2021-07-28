// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Extension methods for working on an <see cref="IJSComponentConfiguration"/>.
    /// </summary>
    public static class JSComponentConfigurationExtensions
    {
        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
        /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
        /// <param name="javaScriptInitializer">Optional. Specifies the identifier for a JavaScript function that will be called to register the custom element. If not specified, the framework will use a default custom element implementation.</param>
        public static void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(this IJSComponentConfiguration configuration, string identifier, string? javaScriptInitializer = null) where TComponent : IComponent
            => RegisterForJavaScript(configuration, typeof(TComponent), identifier, javaScriptInitializer);

        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <param name="configuration">The <see cref="IJSComponentConfiguration"/>.</param>
        /// <param name="componentType">The component type.</param>
        /// <param name="identifier">A unique identifier for the component type that will be used by JavaScript code.</param>
        /// <param name="javaScriptInitializer">Optional. Specifies the identifier for a JavaScript function that will be called to register the custom element. If not specified, the framework will use a default custom element implementation.</param>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JSComponentInterop))]
        public static void RegisterForJavaScript(this IJSComponentConfiguration configuration, [DynamicallyAccessedMembers(Component)] Type componentType, string identifier, string? javaScriptInitializer = null)
            => configuration.JSComponents.Add(componentType, identifier, javaScriptInitializer);
    }
}
