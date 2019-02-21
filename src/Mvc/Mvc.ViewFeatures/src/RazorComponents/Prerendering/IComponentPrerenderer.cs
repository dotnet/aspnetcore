// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Server
{
    /// <summary>
    /// Prerrenders <see cref="IComponent"/> instances.
    /// </summary>
    public interface IComponentPrerenderer
    {
        /// <summary>
        /// Prerrenders the component <see cref="ComponentPrerenderingContext.ComponentType"/>.
        /// </summary>
        /// <param name="context">The context in which the prerrendering is happening.</param>
        /// <returns><see cref="Task{TResult}"/> that will complete when the prerendering is done.</returns>
        Task<IEnumerable<string>> PrerenderComponentAsync(ComponentPrerenderingContext context);
    }
}
