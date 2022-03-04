// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Specifies the contract for execution of a view component.
    /// </summary>
    public interface IViewComponentInvoker
    {
        /// <summary>
        /// Executes the view component specified by <see cref="ViewComponentContext.ViewComponentDescriptor"/>
        /// of <paramref name="context"/> and writes the result to <see cref="ViewComponentContext.Writer"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation of execution.</returns>
        Task InvokeAsync(ViewComponentContext context);
    }
}
