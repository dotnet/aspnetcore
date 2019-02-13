// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for JSON in the MVC framework.
    /// </summary>
    public class MvcJsonOptions : IEnumerable<ICompatibilitySwitch>
    {
        private readonly CompatibilitySwitch<bool> _allowInputFormatterExceptionMessages;
        private readonly ICompatibilitySwitch[] _switches;

        /// <summary>
        /// Creates a new instance of <see cref="MvcJsonOptions"/>.
        /// </summary>
        public MvcJsonOptions()
        {
            _allowInputFormatterExceptionMessages = new CompatibilitySwitch<bool>(nameof(AllowInputFormatterExceptionMessages));

            _switches = new ICompatibilitySwitch[]
            {
                _allowInputFormatterExceptionMessages,
            };
        }

        /// <summary>
        /// Gets or sets a flag to determine whether error messages from JSON deserialization by the 
        /// <see cref="JsonInputFormatter"/> will be added to the <see cref="ModelStateDictionary"/>. The default
        /// value is <c>false</c>, meaning that a generic error message will be used instead.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Error messages in the <see cref="ModelStateDictionary"/> are often communicated to clients, either in HTML
        /// or using <see cref="BadRequestObjectResult"/>. In effect, this setting controls whether clients can receive
        /// detailed error messages about submitted JSON data.
        /// </para>
        /// <para>
        /// This property is associated with a compatibility switch and can provide a different behavior depending on 
        /// the configured compatibility version for the application. See <see cref="CompatibilityVersion"/> for 
        /// guidance and examples of setting the application's compatibility version.
        /// </para>
        /// <para>
        /// Configuring the desired of the value compatibility switch by calling this property's setter will take precedence
        /// over the value implied by the application's <see cref="CompatibilityVersion"/>.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_0"/> then
        /// this setting will have value <c>false</c> unless explicitly configured.
        /// </para>
        /// <para>
        /// If the application's compatibility version is set to <see cref="CompatibilityVersion.Version_2_1"/> or
        /// higher then this setting will have value <c>true</c> unless explicitly configured.
        /// </para>
        /// </remarks>
        public bool AllowInputFormatterExceptionMessages
        {
            get => _allowInputFormatterExceptionMessages.Value;
            set => _allowInputFormatterExceptionMessages.Value = value;
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; } = JsonSerializerSettingsProvider.CreateSerializerSettings();

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)_switches).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _switches.GetEnumerator();
    }
}