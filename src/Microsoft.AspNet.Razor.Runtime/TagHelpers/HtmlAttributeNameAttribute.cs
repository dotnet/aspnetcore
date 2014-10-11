// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to override an <see cref="ITagHelper"/> property's HTML attribute name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class HtmlAttributeNameAttribute : Attribute
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="HtmlAttributeNameAttribute"/> class.
        /// </summary>
        /// <param name="name">HTML attribute name for the associated property.</param>
        public HtmlAttributeNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// HTML attribute name of the associated property.
        /// </summary>
        public string Name { get; private set; }
    }
}