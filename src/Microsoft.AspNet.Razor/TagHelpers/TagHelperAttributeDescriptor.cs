// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class describing a tag helper attribute.
    /// </summary>
    public class TagHelperAttributeDescriptor
    {
        // Internal for testing
        internal TagHelperAttributeDescriptor(string name, PropertyInfo propertyInfo)
            : this(name, propertyInfo.Name, propertyInfo.PropertyType.FullName)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperAttributeDescriptor"/> class.
        /// </summary>
        /// <param name="name">The HTML attribute name.</param>
        /// <param name="propertyName">The name of the CLR property name that corresponds to the HTML
        /// attribute.</param>
        /// <param name="typeName">
        /// The full name of the named (see <paramref name="propertyName"/>) property's
        /// <see cref="System.Type"/>.
        /// </param>
        public TagHelperAttributeDescriptor(string name,
                                            string propertyName,
                                            string typeName)
        {
            Name = name;
            PropertyName = propertyName;
            TypeName = typeName;
        }

        /// <summary>
        /// The HTML attribute name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The name of the CLR property name that corresponds to the HTML attribute name.
        /// </summary>
        public string PropertyName { get; private set; }

        /// <summary>
        /// The full name of the named (see <see name="PropertyName"/>) property's
        /// <see cref="System.Type"/>.
        /// </summary>
        public string TypeName { get; private set; }
    }
}