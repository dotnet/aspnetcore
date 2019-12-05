// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Configures options for binding specific element types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class BindElementAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="BindElementAttribute"/>.
        /// </summary>
        /// <param name="element">The tag name of the element.</param>
        /// <param name="suffix">The suffix value. For example, set this to <c>value</c> for <c>bind-value</c>, or set this to <see langword="null" /> for <c>bind</c>.</param>
        /// <param name="valueAttribute">The name of the value attribute to be bound.</param>
        /// <param name="changeAttribute">The name of an attribute that will register an associated change event.</param>
        public BindElementAttribute(string element, string suffix, string valueAttribute, string changeAttribute)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (valueAttribute == null)
            {
                throw new ArgumentNullException(nameof(valueAttribute));
            }

            if (changeAttribute == null)
            {
                throw new ArgumentNullException(nameof(changeAttribute));
            }

            Element = element;
            ValueAttribute = valueAttribute;
            ChangeAttribute = changeAttribute;
        }
        
        /// <summary>
        /// Gets the tag name of the element.
        /// </summary>
        public string Element { get; }

        /// <summary>
        /// Gets the suffix value.
        /// For example, this will be <c>value</c> to mean <c>bind-value</c>, or <see langword="null" /> to mean <c>bind</c>.
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
