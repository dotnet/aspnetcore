// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Configures options for allowing JavaScript to add root components dynamically.
    /// </summary>
    public interface IJSComponentConfiguration
    {
        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        void RegisterForJavaScript<TComponent>(string identifier) where TComponent : IComponent;
    }
}
