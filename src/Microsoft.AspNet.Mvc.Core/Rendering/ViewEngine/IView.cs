// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
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
        Task RenderAsync([NotNull] ViewContext context);
    }
}
