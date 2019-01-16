// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Configures options for binding subtypes of an HTML <code>input</code> element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class BindInputElementAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="BindInputElementAttribute"/>.
        /// </summary>
        /// <param name="type">The value of the element's <code>type</code> attribute.</param>
        /// <param name="suffix">The suffix value.</param>
        /// <param name="valueAttribute">The name of the value attribute to be bound.</param>
        /// <param name="changeAttribute">The name of an attribute that will register an associated change event.</param>
        public BindInputElementAttribute(string type, string suffix, string valueAttribute, string changeAttribute)
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
        }

        /// <summary>
        /// Gets the value of the element's <code>type</code> attribute.
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
    }
}
