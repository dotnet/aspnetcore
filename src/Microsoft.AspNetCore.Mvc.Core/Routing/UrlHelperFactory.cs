// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// A default implementation of <see cref="IUrlHelperFactory"/>.
    /// </summary>
    public class UrlHelperFactory : IUrlHelperFactory
    {
        /// <inheritdoc />
        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            return new UrlHelper(context);
        }
    }
}
