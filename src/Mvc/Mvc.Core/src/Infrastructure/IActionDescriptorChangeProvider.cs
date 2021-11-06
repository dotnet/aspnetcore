// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Provides a way to signal invalidation of the cached collection of <see cref="Abstractions.ActionDescriptor" /> from an
/// <see cref="IActionDescriptorCollectionProvider"/>.
/// </summary>
/// <remarks>
/// The change token returned from <see cref="GetChangeToken"/> is only for use inside the MVC infrastructure.
/// Use <see cref="ActionDescriptorCollectionProvider.GetChangeToken"/> to be notified of <see cref="ActionDescriptor"/>
/// changes.
/// </remarks>
public interface IActionDescriptorChangeProvider
{
    /// <summary>
    /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Abstractions.ActionDescriptor"/>
    /// instances.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    /// <remarks>
    /// The change token returned from <see cref="GetChangeToken"/> is only for use inside the MVC infrastructure.
    /// Use <see cref="ActionDescriptorCollectionProvider.GetChangeToken"/> to be notified of <see cref="ActionDescriptor"/>
    /// changes.
    /// </remarks>
    IChangeToken GetChangeToken();
}
