// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Configures options for binding subtypes of an HTML <c>input</c> element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class BindInputElementAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="BindInputElementAttribute"/>.
        /// </summary>
        /// <param name="type">The value of the element's <c>type</c> attribute.</param>
        /// <param name="suffix">The suffix value.</param>
        /// <param name="valueAttribute">The name of the value attribute to be bound.</param>
        /// <param name="changeAttribute">The name of an attribute that will register an associated change event.</param>
        /// <param name="isInvariantCulture">
        /// Determines whether binding will use <see cref="CultureInfo.InvariantCulture" /> or <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <param name="format">
        /// An optional format to use when converting values. 
        /// </param>
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute, bool isInvariantCulture, string format)
        {
            if (valueAttribute == null)
            {
                throw new ArgumentNullException(nameof(valueAttribute));
            }

            if (changeAttribute == null)
            {
                throw new ArgumentNullException(nameof(changeAttribute));
            }

            Type = type;
            Suffix = suffix;
            ValueAttribute = valueAttribute;
            ChangeAttribute = changeAttribute;
            IsInvariantCulture = isInvariantCulture;
            Format = format;
        }

        /// <summary>
        /// Gets the value of the element's <c>type</c> attribute.
        /// </summary>
        public string Type { get; }
        
        /// <summary>
        /// Gets the suffix value.
        /// </summary>
        public string Suffix { get; }

        /// <summary>
        /// Gets the name of the value attribute to be bound.
        /// </summary>
        public string ValueAttribute { get; }

        /// <summary>
        /// Gets the name of an attribute that will register an associated change event.
        /// </summary>
        public string ChangeAttribute { get; }

        /// <summary>
        /// Gets a value that determines whether binding will use <see cref="CultureInfo.InvariantCulture" /> or
        /// <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public bool IsInvariantCulture { get; }

        /// <summary>
        /// Gets an optional format to use when converting values.
        /// </summary>
        public string Format { get; }
    }
}
