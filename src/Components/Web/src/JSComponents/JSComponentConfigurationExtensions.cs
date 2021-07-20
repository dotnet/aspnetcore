// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Builder
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
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JSComponentInterop))]
        public static void RegisterForJavaScript<[DynamicallyAccessedMembers(Component)] TComponent>(this IJSComponentConfiguration configuration, string identifier) where TComponent : IComponent
            => configuration.JSComponents.Add(identifier, typeof(TComponent));
    }
}
