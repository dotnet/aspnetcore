// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private readonly CompatibilitySwitch<bool> _suppressTempDataAttributePrefix;
        private readonly CompatibilitySwitch<bool> _allowRenderingMaxLengthAttribute;
        private readonly ICompatibilitySwitch[] _switches;
        private HtmlHelperOptions _htmlHelperOptions = new HtmlHelperOptions();

        public MvcViewOptions()
        {
            _suppressTempDataAttributePrefix = new CompatibilitySwitch<bool>(nameof(SuppressTempDataAttributePrefix));
            _allowRenderingMaxLengthAttribute = new CompatibilitySwitch<bool>(nameof(AllowRenderingMaxLengthAttribute));
            _switches = new[]
            {
                _suppressTempDataAttributePrefix,
                _allowRenderingMaxLengthAttribute
            };
        }

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
        /// <para>
        /// Gets or sets a value that determines if the <see cref="ITempDataDictionary"/> keys for
        /// properties annotated with <see cref="TempDataAttribute"/> include the prefix <c>TempDataProperty-</c>.
        /// </para>
        /// <para>
        /// When <see cref="TempDataAttribute.Key"/> is not specified, the lookup key for properties annotated
        /// with <see cref="TempDataAttribute"/> is derived from the property name. In releases prior to ASP.NET Core 2.1,
        /// the calculated key was the property name prefixed by the value <c>TempDataProperty-</c>.
        /// e.g. <c>TempDataProperty-SuccessMessage</c>. When this option is <c>true</c>, the calculated key for the property is
        /// the property name e.g. <c>SuccessMessage</c>.
        /// </para>
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        public bool SuppressTempDataAttributePrefix
        {
            get => _suppressTempDataAttributePrefix.Value;
            set => _suppressTempDataAttributePrefix.Value = value;
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the maxlength attribute should be rendered for compatible HTML elements,
        /// when they're bound to models marked with either
        /// <see cref="StringLengthAttribute"/> or <see cref="MaxLengthAttribute"/> attributes.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// If both attributes are specified, the one with the smaller value will be used for the rendered `maxlength` attribute.
        /// </remarks>
        public bool AllowRenderingMaxLengthAttribute
        {
            get => _allowRenderingMaxLengthAttribute.Value;
            set => _allowRenderingMaxLengthAttribute.Value = value;
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

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
