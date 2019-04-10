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
        /// Gets or sets a value that determines if <see cref="MvcDataAnnotationsLocalizationOptions.DataAnnotationLocalizerProvider"/> should be used while localizing <see cref="Enum"/> types.
        /// If set to <c>true</c> <see cref="MvcDataAnnotationsLocalizationOptions.DataAnnotationLocalizerProvider"/> will be used in localizing <see cref="Enum"/> types.
        /// If set to <c>false</c> the localization will search for values in resource files for the <see cref="Enum"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired value of the compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> or <see cref="CompatibilityVersion.Version_2_1"/> then
        /// this setting will have the value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_2"/> or
        /// higher then this setting will have the value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowDataAnnotationsLocalizationForEnumDisplayAttributes
        {
            get => _allowDataAnnotationsLocalizationForEnumDisplayAttributes.Value;
            set => _allowDataAnnotationsLocalizationForEnumDisplayAttributes.Value = value;
        }

        public IEnumerator<ICompatibilitySwitch> GetEnumerator() => ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}
