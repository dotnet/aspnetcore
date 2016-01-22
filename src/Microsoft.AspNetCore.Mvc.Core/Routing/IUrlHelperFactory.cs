// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Routing
{
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
}
