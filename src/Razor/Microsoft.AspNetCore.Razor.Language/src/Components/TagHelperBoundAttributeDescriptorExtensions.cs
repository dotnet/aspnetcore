// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class TagHelperBoundAttributeDescriptorExtensions
{
    public static bool IsDelegateProperty(this BoundAttributeDescriptor attribute)
    {
        var key = ComponentMetadata.Component.DelegateSignatureKey;
        return
            attribute.Metadata.TryGetValue(key, out var value) &&
            string.Equals(value, bool.TrueString);
    }

    /// <summary>
    /// Gets a value indicating whether the attribute is of type <c>EventCallback</c> or
    /// <c>EventCallback{T}</c>
    /// </summary>
    /// <param name="attribute">The <see cref="BoundAttributeDescriptor"/>.</param>
    /// <returns><c>true</c> if the attribute is an event callback, otherwise <c>false</c>.</returns>
    public static bool IsEventCallbackProperty(this BoundAttributeDescriptor attribute)
    {
        var key = ComponentMetadata.Component.EventCallbackKey;
        return
            attribute.Metadata.TryGetValue(key, out var value) &&
            string.Equals(value, bool.TrueString);
    }

    public static bool IsGenericTypedProperty(this BoundAttributeDescriptor attribute)
    {
        return
            attribute.Metadata.TryGetValue(ComponentMetadata.Component.GenericTypedKey, out var value) &&
            string.Equals(value, bool.TrueString);
    }

    public static bool IsTypeParameterProperty(this BoundAttributeDescriptor attribute)
    {
        return
           attribute.Metadata.TryGetValue(ComponentMetadata.Component.TypeParameterKey, out var value) &&
           string.Equals(value, bool.TrueString);
    }

    public static bool IsCascadingTypeParameterProperty(this BoundAttributeDescriptor attribute)
    {
        return
          attribute.Metadata.TryGetValue(ComponentMetadata.Component.TypeParameterIsCascadingKey, out var value) &&
          string.Equals(value, bool.TrueString);
    }

    public static bool IsWeaklyTyped(this BoundAttributeDescriptor attribute)
    {
        var key = ComponentMetadata.Component.WeaklyTypedKey;
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
        var key = ComponentMetadata.Component.ChildContentKey;
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
        var key = ComponentMetadata.Component.ChildContentKey;
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
        var key = ComponentMetadata.Component.ChildContentParameterNameKey;
        return
            attribute.Metadata.TryGetValue(key, out var value) &&
            string.Equals(value, bool.TrueString);
    }
}
