// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="Type"/>s.
    /// </summary>
    public static class TagHelperDescriptorFactory
    {
        private const string TagHelperNameEnding = "TagHelper";

        // TODO: Investigate if we should cache TagHelperDescriptors for types:
        // https://github.com/aspnet/Razor/issues/165

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to create a <see cref="TagHelperDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperDescriptor"/> that describes the given <paramref name="type"/>.</returns>
        public static TagHelperDescriptor CreateDescriptor(Type type)
        {
            var tagName = GetTagName(type);
            var typeName = type.FullName;
            var attributeDescriptors = GetAttributeDescriptors(type);
            var contentBehavior = GetContentBehavior(type);

            return new TagHelperDescriptor(tagName,
                                           typeName,
                                           contentBehavior,
                                           attributeDescriptors);
        }

        // TODO: Make this method support TagNameAttribute tag names: https://github.com/aspnet/Razor/issues/120
        private static string GetTagName(Type tagHelperType)
        {
            var name = tagHelperType.Name;

            if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
            }

            return name;
        }

        private static IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var properties = typeInfo.DeclaredProperties.Where(IsValidProperty);
            var attributeDescriptors = properties.Select(ToAttributeDescriptor);

            return attributeDescriptors;
        }

        // TODO: Make the HTML attribute name support names from a AttributeNameAttribute: 
        // https://github.com/aspnet/Razor/issues/121
        private static TagHelperAttributeDescriptor ToAttributeDescriptor(PropertyInfo property)
        {
            return new TagHelperAttributeDescriptor(property.Name, property);
        }

        private static ContentBehavior GetContentBehavior(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var contentBehaviorAttribute = typeInfo.GetCustomAttribute<ContentBehaviorAttribute>(inherit: false);

            return contentBehaviorAttribute != null ? 
                contentBehaviorAttribute.ContentBehavior :
                ContentBehavior.None;
        }

        private static bool IsValidProperty(PropertyInfo property)
        {
            return property.GetMethod != null &&
                   property.GetMethod.IsPublic &&
                   property.SetMethod != null &&
                   property.SetMethod.IsPublic;
        }
    }
}