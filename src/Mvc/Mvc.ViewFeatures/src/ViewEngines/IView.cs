// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewEngines
{
    /// <summary>
    /// Specifies the contract for a view.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Gets the path of the view as resolved by the <see cref="IViewEngine"/>.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Asynchronously renders the view using the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewContext"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion renders the view.</returns>
        Task RenderAsync(ViewContext context);
    }
}
