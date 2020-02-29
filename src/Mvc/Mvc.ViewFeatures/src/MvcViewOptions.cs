// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for views in the MVC framework.
    /// </summary>
    public class MvcViewOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();
        private HtmlHelperOptions _htmlHelperOptions = new HtmlHelperOptions();

        /// <summary>
        /// Gets or sets programmatic configuration for the HTML helpers and <see cref="Rendering.ViewContext"/>.
        /// </summary>
        public HtmlHelperOptions HtmlHelperOptions
        {
            get => _htmlHelperOptions;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _htmlHelperOptions = value;
            }
        }

        /// <summary>
        /// Gets a list <see cref="IViewEngine"/>s used by this application.
        /// </summary>
        public IList<IViewEngine> ViewEngines { get; } = new List<IViewEngine>();

        /// <summary>
        /// Gets a list of <see cref="IClientModelValidatorProvider"/> instances.
        /// </summary>
        public IList<IClientModelValidatorProvider> ClientModelValidatorProviders { get; } =
            new List<IClientModelValidatorProvider>();

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator() => _switches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
