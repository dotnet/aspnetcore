// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// Provides programmatic configuration for DataAnnotations localization in the MVC framework.
    /// </summary>
    public class MvcDataAnnotationsLocalizationOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly CompatibilitySwitch<bool> _allowDataAnnotationsLocalizationForEnumDisplayAttributes;
        private readonly ICompatibilitySwitch[] _switches;

        /// <summary>
        /// The delegate to invoke for creating <see cref="IStringLocalizer"/>.
        /// </summary>
        public Func<Type, IStringLocalizerFactory, IStringLocalizer> DataAnnotationLocalizerProvider;

        /// <summary>
        /// Instantiates a new instance of the <see cref="MvcDataAnnotationsLocalizationOptions"/> class.
        /// </summary>
        public MvcDataAnnotationsLocalizationOptions()
        {
            _allowDataAnnotationsLocalizationForEnumDisplayAttributes = new CompatibilitySwitch<bool>(nameof(AllowDataAnnotationsLocalizationForEnumDisplayAttributes));

            _switches = new ICompatibilitySwitch[]
            {
                _allowDataAnnotationsLocalizationForEnumDisplayAttributes
            };
        }

        /// <summary>
        /// Gets or sets a value that determines if <see cref="DataAnnotationLocalizerProvider"/> should be used while localizing <see cref="Enum"/> types.
        /// If set to <c>true</c> <see cref="DataAnnotationLocalizerProvider"/> will be used in localizing <see cref="Enum"/> types.
        /// If set to <c>false</c> the localization will search for values in resource files for the <see cref="Enum"/>.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        public bool AllowDataAnnotationsLocalizationForEnumDisplayAttributes
        {
            get => _allowDataAnnotationsLocalizationForEnumDisplayAttributes.Value;
            set => _allowDataAnnotationsLocalizationForEnumDisplayAttributes.Value = value;
        }

        public IEnumerator<ICompatibilitySwitch> GetEnumerator() => ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
