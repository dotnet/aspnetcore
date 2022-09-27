// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// A factory for creating <see cref="IUrlHelper"/> instances.
/// </summary>
public interface IUrlHelperFactory
{
    /// <summary>
    /// Gets an <see cref="IUrlHelper"/> for the request associated with <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <returns>An <see cref="IUrlHelper"/> for the request associated with <paramref name="context"/></returns>
    IUrlHelper GetUrlHelper(ActionContext context);
}
