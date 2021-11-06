// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class TagHelperDescriptorExtensions
{
    public static bool IsAnyComponentDocumentTagHelper(this TagHelperDescriptor tagHelper)
    {
        return tagHelper.IsComponentTagHelper() || tagHelper.Metadata.ContainsKey(ComponentMetadata.SpecialKindKey);
    }

    public static bool IsBindTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var kind) &&
            string.Equals(ComponentMetadata.Bind.TagHelperKind, kind);
    }

    public static bool IsFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.IsBindTagHelper() &&
            tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.FallbackKey, out var fallback) &&
            string.Equals(bool.TrueString, fallback);
    }

    public static bool IsGenericTypedComponent(this TagHelperDescriptor tagHelper)
    {
        return
            IsComponentTagHelper(tagHelper) &&
            tagHelper.Metadata.TryGetValue(ComponentMetadata.Component.GenericTypedKey, out var value) &&
            string.Equals(bool.TrueString, value);
    }

    public static bool IsInputElementBindTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.IsBindTagHelper() &&
            tagHelper.TagMatchingRules.Count == 1 &&
            string.Equals("input", tagHelper.TagMatchingRules[0].TagName);
    }

    public static bool IsInputElementFallbackBindTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.IsInputElementBindTagHelper() &&
            !tagHelper.Metadata.ContainsKey(ComponentMetadata.Bind.TypeAttribute);
    }

    public static string GetValueAttributeName(this TagHelperDescriptor tagHelper)
    {
        tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.ValueAttribute, out var result);
        return result;
    }

    public static string GetChangeAttributeName(this TagHelperDescriptor tagHelper)
    {
        tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.ChangeAttribute, out var result);
        return result;
    }

    public static string GetExpressionAttributeName(this TagHelperDescriptor tagHelper)
    {
        tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.ExpressionAttribute, out var result);
        return result;
    }

    /// <summary>
    /// Gets a value that indicates where the tag helper is a bind tag helper with a default
    /// culture value of <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>
    /// <c>true</c> if this tag helper is a bind tag helper and defaults in <see cref="CultureInfo.InvariantCulture"/>
    /// </returns>
    public static bool IsInvariantCultureBindTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.IsInvariantCulture, out var text) &&
            bool.TryParse(text, out var result) &&
            result;
    }

    /// <summary>
    /// Gets the default format value for a bind tag helper.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The format, or <c>null</c>.</returns>
    public static string GetFormat(this TagHelperDescriptor tagHelper)
    {
        tagHelper.Metadata.TryGetValue(ComponentMetadata.Bind.Format, out var result);
        return result;
    }

    public static bool IsChildContentTagHelper(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper.IsChildContentTagHelperCache is bool value)
        {
            return value;
        }

        value = tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var specialKey) &&
            string.Equals(specialKey, ComponentMetadata.ChildContent.TagHelperKind, StringComparison.Ordinal);

        tagHelper.IsChildContentTagHelperCache = value;
        return value;
    }

    public static bool IsComponentTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            string.Equals(tagHelper.Kind, ComponentMetadata.Component.TagHelperKind) &&
            !tagHelper.Metadata.ContainsKey(ComponentMetadata.SpecialKindKey);
    }

    public static bool IsEventHandlerTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var kind) &&
            string.Equals(ComponentMetadata.EventHandler.TagHelperKind, kind);
    }

    public static bool IsKeyTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var kind) &&
            string.Equals(ComponentMetadata.Key.TagHelperKind, kind);
    }

    public static bool IsSplatTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var kind) &&
            string.Equals(ComponentMetadata.Splat.TagHelperKind, kind);
    }

    public static bool IsRefTagHelper(this TagHelperDescriptor tagHelper)
    {
        return
            tagHelper.Metadata.TryGetValue(ComponentMetadata.SpecialKindKey, out var kind) &&
            string.Equals(ComponentMetadata.Ref.TagHelperKind, kind);
    }

    /// <summary>
    /// Gets whether the component matches a tag with a fully qualified name.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    public static bool IsComponentFullyQualifiedNameMatch(this TagHelperDescriptor tagHelper)
    {
        if (tagHelper.IsComponentFullyQualifiedNameMatchCache is bool value)
        {
            return value;
        }

        value = tagHelper.Metadata.TryGetValue(ComponentMetadata.Component.NameMatchKey, out var matchType) &&
            string.Equals(ComponentMetadata.Component.FullyQualifiedNameMatch, matchType);
        tagHelper.IsComponentFullyQualifiedNameMatchCache = value;
        return value;
    }

    public static string GetEventArgsType(this TagHelperDescriptor tagHelper)
    {
        tagHelper.Metadata.TryGetValue(ComponentMetadata.EventHandler.EventArgsType, out var result);
        return result;
    }

    /// <summary>
    /// Gets the set of component attributes that can accept child content (<c>RenderFragment</c> or <c>RenderFragment{T}</c>).
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The child content attributes</returns>
    public static IEnumerable<BoundAttributeDescriptor> GetChildContentProperties(this TagHelperDescriptor tagHelper)
    {
        for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
        {
            var attribute = tagHelper.BoundAttributes[i];
            if (attribute.IsChildContentProperty())
            {
                yield return attribute;
            }
        }
    }

    /// <summary>
    /// Gets the set of component attributes that represent generic type parameters of the component type.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>The type parameter attributes</returns>
    public static IEnumerable<BoundAttributeDescriptor> GetTypeParameters(this TagHelperDescriptor tagHelper)
    {
        for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
        {
            var attribute = tagHelper.BoundAttributes[i];
            if (attribute.IsTypeParameterProperty())
            {
                yield return attribute;
            }
        }
    }

    /// <summary>
    /// Gets a flag that indicates whether the corresponding component supplies any cascading
    /// generic type parameters to descendants.
    /// </summary>
    /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/>.</param>
    /// <returns>True if it does supply one or more generic type parameters to descendants; false otherwise.</returns>
    public static bool SuppliesCascadingGenericParameters(this TagHelperDescriptor tagHelper)
    {
        for (var i = 0; i < tagHelper.BoundAttributes.Count; i++)
        {
            var attribute = tagHelper.BoundAttributes[i];
            if (attribute.IsCascadingTypeParameterProperty())
            {
                return true;
            }
        }

        return false;
    }
}
