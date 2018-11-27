// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Provides a way to signal invalidation of the cached collection of <see cref="Abstractions.ActionDescriptor" /> from an
    /// <see cref="IActionDescriptorCollectionProvider"/>.
    /// </summary>
    public interface IActionDescriptorChangeProvider
    {
        /// <summary>
        /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Abstractions.ActionDescriptor"/>
        /// instances.
        /// </summary>
        /// <returns>The <see cref="IChangeToken"/>.</returns>
        IChangeToken GetChangeToken();
    }
}