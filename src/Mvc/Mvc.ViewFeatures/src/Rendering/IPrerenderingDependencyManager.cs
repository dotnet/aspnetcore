// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Manages prerendering dependencies to ensure that components are prerendered in the
    /// desired order.
    /// </summary>
    public interface IPrerenderingDependencyManager
    {
        /// <summary>
        /// Indicates that the specified component type must be prerendered before
        /// other root components are prerendered.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        void DependsOn<TComponent>();

        /// <summary>
        /// Indicates that the specified component type must be prerendered before
        /// other root components are prerendered.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="parameters">The parameters to be passed to the component when it is prerendered.</param>
        void DependsOn<TComponent>(IDictionary<string, object> parameters);
    }
}
