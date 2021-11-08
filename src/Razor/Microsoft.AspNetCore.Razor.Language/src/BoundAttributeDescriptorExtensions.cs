// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class BoundAttributeDescriptorExtensions
{
    public static string GetPropertyName(this BoundAttributeDescriptor attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        attribute.Metadata.TryGetValue(TagHelperMetadata.Common.PropertyName, out var propertyName);
        return propertyName;
    }

    public static bool IsDefaultKind(this BoundAttributeDescriptor attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        return string.Equals(attribute.Kind, TagHelperConventions.DefaultKind, StringComparison.Ordinal);
    }

    internal static bool ExpectsStringValue(this BoundAttributeDescriptor attribute, string name)
    {
        if (attribute.IsStringProperty)
        {
            return true;
        }

        var isIndexerNameMatch = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(name, attribute);
        return isIndexerNameMatch && attribute.IsIndexerStringProperty;
    }

    internal static bool ExpectsBooleanValue(this BoundAttributeDescriptor attribute, string name)
    {
        if (attribute.IsBooleanProperty)
        {
            return true;
        }

        var isIndexerNameMatch = TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(name, attribute);
        return isIndexerNameMatch && attribute.IsIndexerBooleanProperty;
    }

    public static bool IsDirectiveAttribute(this BoundAttributeDescriptor attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        return
            attribute.Metadata.TryGetValue(ComponentMetadata.Common.DirectiveAttribute, out var value) &&
            string.Equals(bool.TrueString, value);
    }

    public static bool IsDefaultKind(this BoundAttributeParameterDescriptor parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        return string.Equals(parameter.Kind, TagHelperConventions.DefaultKind, StringComparison.Ordinal);
    }

    public static string GetPropertyName(this BoundAttributeParameterDescriptor parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        parameter.Metadata.TryGetValue(TagHelperMetadata.Common.PropertyName, out var propertyName);
        return propertyName;
    }
}
