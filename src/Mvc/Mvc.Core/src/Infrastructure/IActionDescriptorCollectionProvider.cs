// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Provides the currently cached collection of <see cref="Abstractions.ActionDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// The default implementation internally caches the collection and uses
    /// <see cref="IActionDescriptorChangeProvider"/> to invalidate this cache, incrementing
    /// <see cref="ActionDescriptorCollection.Version"/> the collection is reconstructed.
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