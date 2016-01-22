// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Provides the currently cached collection of <see cref="Abstractions.ActionDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// The default implementation, does not update the cache, it is up to the user
    /// to create or use an implementation that can update the available actions in
    /// the application. The implementor is also responsible for updating the
    /// <see cref="ActionDescriptorCollection.Version"/> in a thread safe way.
    ///
    /// Default consumers of this service, are aware of the version and will recache
    /// data as appropriate, but rely on the version being unique.
    /// </remarks>
    public interface IActionDescriptorCollectionProvider
    {
        /// <summary>
        /// Returns the current cached <see cref="ActionDescriptorCollection"/>
        /// </summary>
        ActionDescriptorCollection ActionDescriptors { get; }
    }
}