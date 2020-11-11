// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Interface that exposts the ability to create an <see cref="IViewComponentInvoker"/>.
    /// </summary>
    public interface IViewComponentInvokerFactory
    {
        /// <summary>
        /// Creates a <see cref="IViewComponentInvoker"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>The <see cref="IViewComponentInvoker"/>.</returns>
        IViewComponentInvoker CreateInstance(ViewComponentContext context);
    }
}
