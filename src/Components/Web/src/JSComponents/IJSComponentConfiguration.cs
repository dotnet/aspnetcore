// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Web.JSComponents;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Configures options for allowing JavaScript to add root components dynamically.
    /// </summary>
    public interface IJSComponentConfiguration
    {
        /// <summary>
        /// Gets the store of configuration options that allow JavaScript to add root components dynamically.
        /// </summary>
        JSComponentConfigurationStore JSComponents { get; }

        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JSComponentInterop))]
        public void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(string identifier) where TComponent : IComponent
            => JSComponents.Add(identifier, typeof(TComponent));
    }
}
