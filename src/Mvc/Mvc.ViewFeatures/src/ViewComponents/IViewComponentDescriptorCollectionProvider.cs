// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Provides the currently cached collection of <see cref="ViewComponentDescriptor"/>.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation does not update the cache, it is up to the user
/// to create or use an implementation that can update the available view components in
/// the application. The implementor is also responsible for updating the
/// <see cref="ViewComponentDescriptorCollection.Version"/> in a thread safe way.
/// </para>
/// <para>
/// Default consumers of this service, are aware of the version and will recache
/// data as appropriate, but rely on the version being unique.
/// </para>
/// </remarks>
public interface IViewComponentDescriptorCollectionProvider
{
    /// <summary>
    /// Returns the current cached <see cref="ViewComponentDescriptorCollection"/>.
    /// </summary>
    ViewComponentDescriptorCollection ViewComponents { get; }
}
