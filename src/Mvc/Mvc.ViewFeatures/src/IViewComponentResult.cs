// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Result type of a <see cref="ViewComponent"/>.
    /// </summary>
    public interface IViewComponentResult
    {
        /// <summary>
        /// Executes the result of a <see cref="ViewComponent"/> using the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
        void Execute(ViewComponentContext context);

        /// <summary>
        /// Asynchronously executes the result of a <see cref="ViewComponent"/> using the specified
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous execution.</returns>
        Task ExecuteAsync(ViewComponentContext context);
    }
}
