// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for views in the MVC framework.
    /// </summary>
    public class MvcViewOptions
    {
        /// <summary>
        /// Gets or sets programmatic configuration for the HTML helpers and <see cref="ViewContext"/>.
        /// </summary>
        public HtmlHelperOptions HtmlHelperOptions { get;[param: NotNull] set; } = new HtmlHelperOptions();

        /// <summary>
        /// Gets a list <see cref="IViewEngine"/>s used by this application.
        /// </summary>
        public IList<IViewEngine> ViewEngines { get; } = new List<IViewEngine>();

        /// <summary>
        /// Gets a list of <see cref="IClientModelValidatorProvider"/> instances.
        /// </summary>
        public IList<IClientModelValidatorProvider> ClientModelValidatorProviders { get; } =
            new List<IClientModelValidatorProvider>();
    }
}