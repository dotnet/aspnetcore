// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// Provides configuration for RazorPages.
    /// </summary>
    public class RazorPagesOptions
    {
        /// <summary>
        /// Gets a list of <see cref="IPageModelConvention"/> instances that will be applied to
        /// the <see cref="PageModel"/> when discovering Razor Pages.
        /// </summary>
        public IList<IPageModelConvention> Conventions { get; } = new List<IPageModelConvention>();
    }
}