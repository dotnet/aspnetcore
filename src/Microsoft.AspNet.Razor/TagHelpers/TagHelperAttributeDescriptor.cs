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
        /// <summary>
        /// Instantiates a new <see cref="TagHelperAttributeDescriptor"/> class.
        /// </summary>
        /// <param name="attributeName">The HTML attribute name.</param>
        /// <param name="propertyInfo">The <see cref="System.Reflection.PropertyInfo"/> for the tag
        /// helper attribute</param>
        public TagHelperAttributeDescriptor(string attributeName, PropertyInfo propertyInfo)
            : this(attributeName, propertyInfo.Name, propertyInfo.PropertyType.FullName)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TagHelperAttributeDescriptor"/> class.
        /// </summary>
        /// <param name="attributeName">The HTML attribute name.</param>
        /// <param name="attributePropertyName">The name of the CLR property name that corresponds to the HTML 
        /// attribute name.</param>
        /// <param name="attributeTypeName">
        /// The full name of the <see cref="System.Type"/> that corresponds to the HTML attribute.
        /// </param>
        public TagHelperAttributeDescriptor(string attributeName,
                                            string attributePropertyName,
                                            string attributeTypeName)
        {
            AttributeName = attributeName;
            AttributePropertyName = attributePropertyName;
            AttributeTypeName = attributeTypeName;
        }

        /// <summary>
        /// The HTML attribute name.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// The name of the CLR property name that corresponds to the HTML attribute name.
        /// </summary>
        public string AttributePropertyName { get; private set; }

        /// <summary>
        /// The full name of the <see cref="System.Type"/> that corresponds to the HTML attribute.
        /// </summary>
        public string AttributeTypeName { get; private set; }
    }
}