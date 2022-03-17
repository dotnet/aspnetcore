// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A base class for <see cref="IActionDescriptorCollectionProvider"/> which also provides an <see cref="IChangeToken"/>
/// for reactive notifications of <see cref="ActionDescriptor"/> changes.
/// </summary>
/// <remarks>
/// <see cref="ActionDescriptorCollectionProvider"/> is used as a base class by the default implementation of
/// <see cref="IActionDescriptorCollectionProvider"/>. To retrieve an instance of <see cref="ActionDescriptorCollectionProvider"/>,
/// obtain the <see cref="IActionDescriptorCollectionProvider"/> from the dependency injection provider and
/// downcast to <see cref="ActionDescriptorCollectionProvider"/>.
/// </remarks>
public abstract class ActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
{
    /// <summary>
    /// Returns the current cached <see cref="ActionDescriptorCollection"/>
    /// </summary>
    public abstract ActionDescriptorCollection ActionDescriptors { get; }

    /// <summary>
    /// Gets an <see cref="IChangeToken"/> that will be signaled after the <see cref="ActionDescriptors"/>
    /// collection has changed.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public abstract IChangeToken GetChangeToken();
}
