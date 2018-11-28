// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal static class TagHelperBoundAttributeDescriptorExtensions
    {
        public static bool IsDelegateProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = BlazorMetadata.Component.DelegateSignatureKey;
            return 
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        public static bool IsGenericTypedProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }
            
            return
                attribute.Metadata.TryGetValue(BlazorMetadata.Component.GenericTypedKey, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        public static bool IsTypeParameterProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return
                attribute.Metadata.TryGetValue(BlazorMetadata.Component.TypeParameterKey, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        public static bool IsWeaklyTyped(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = BlazorMetadata.Component.WeaklyTypedKey;
            return
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        /// <summary>
        /// Gets a value that indicates whether the property is a child content property. Properties are
        /// considered child content if they have the type <c>RenderFragment</c> or <c>RenderFragment{T}</c>.
        /// </summary>
        /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
        /// <returns>Returns <c>true</c> if the property is child content, otherwise <c>false</c>.</returns>
        public static bool IsChildContentProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = BlazorMetadata.Component.ChildContentKey;
            return
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        /// <summary>
        /// Gets a value that indicates whether the property is a child content property. Properties are
        /// considered child content if they have the type <c>RenderFragment</c> or <c>RenderFragment{T}</c>.
        /// </summary>
        /// <param name="attribute">The <see cref="BoundAttributeDescriptorBuilder"/>.</param>
        /// <returns>Returns <c>true</c> if the property is child content, otherwise <c>false</c>.</returns>
        public static bool IsChildContentProperty(this BoundAttributeDescriptorBuilder attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = BlazorMetadata.Component.ChildContentKey;
            return
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }

        /// <summary>
        /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
        /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
        /// </summary>
        /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
        /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
        public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return attribute.IsChildContentProperty() &&
                !string.Equals(attribute.TypeName, ComponentsApi.RenderFragment.FullTypeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets a value that indicates whether the property is a parameterized child content property. Properties are
        /// considered parameterized child content if they have the type <c>RenderFragment{T}</c> (for some T).
        /// </summary>
        /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
        /// <returns>Returns <c>true</c> if the property is parameterized child content, otherwise <c>false</c>.</returns>
        public static bool IsParameterizedChildContentProperty(this BoundAttributeDescriptorBuilder attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return attribute.IsChildContentProperty() &&
                !string.Equals(attribute.TypeName, ComponentsApi.RenderFragment.FullTypeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets a value that indicates whether the property is used to specify the name of the parameter
        /// for a parameterized child content property.
        /// </summary>
        /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
        /// <returns>
        /// Returns <c>true</c> if the property specifies the name of a parameter for a parameterized child content,
        /// otherwise <c>false</c>.
        /// </returns>
        public static bool IsChildContentParameterNameProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = BlazorMetadata.Component.ChildContentParameterNameKey;
            return
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }
    }
}
