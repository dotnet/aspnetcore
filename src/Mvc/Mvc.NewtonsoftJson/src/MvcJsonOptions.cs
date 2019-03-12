// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for JSON in the MVC framework.
    /// </summary>
    [Obsolete("This class is obsolete. Use the MvcNewtonsoftJsonOptions class instead.")]
    public class MvcJsonOptions : IEnumerable<ICompatibilitySwitch>
    {
        /// <summary>
        /// Creates a new instance of <see cref="MvcJsonOptions"/>.
        /// </summary>
        public MvcJsonOptions()
        {
        }

        /// <summary>
        /// Gets or sets a flag to determine whether error messages from JSON deserialization by the
        /// will be added to the <see cref="ModelStateDictionary"/>. If <see langword="false"/>, a
        /// generic error message will be used instead.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// Error messages in the <see cref="ModelStateDictionary"/> are often communicated to clients, either in HTML
        /// or using <see cref="BadRequestObjectResult"/>. In effect, this setting controls whether clients can receive
        /// detailed error messages about submitted JSON data.
        /// </remarks>
        [Obsolete("This property is obsolete. Use the MvcNewtonsoftJsonOptions.AllowInputFormatterExceptionMessages property instead.")]
        public bool AllowInputFormatterExceptionMessages
        {
            get => Proxy.AllowInputFormatterExceptionMessages;
            set => Proxy.AllowInputFormatterExceptionMessages = value;
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
        /// </summary>
        [Obsolete("This property is obsolete. Use the MvcNewtonsoftJsonOptions.SerializerSettings property instead.")]
        public JsonSerializerSettings SerializerSettings => Proxy.SerializerSettings;

        /// <summary>
        /// Gets or sets the <see cref="MvcNewtonsoftJsonOptions"/> to proxy values for.
        /// </summary>
        internal MvcNewtonsoftJsonOptions Proxy { get; set; }

        IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
        {
            return ((IEnumerable<ICompatibilitySwitch>)Proxy).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<ICompatibilitySwitch>)Proxy).GetEnumerator();
    }
}
