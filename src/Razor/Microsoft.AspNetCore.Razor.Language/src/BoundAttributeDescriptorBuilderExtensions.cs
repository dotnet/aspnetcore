// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class BoundAttributeDescriptorBuilderExtensions
{
    public static void SetPropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (propertyName == null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        builder.Metadata[TagHelperMetadata.Common.PropertyName] = propertyName;
    }

    public static string GetPropertyName(this BoundAttributeDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Metadata.TryGetValue(TagHelperMetadata.Common.PropertyName, out var value))
        {
            return value;
        }

        return null;
    }

    public static void AsDictionary(
        this BoundAttributeDescriptorBuilder builder,
        string attributeNamePrefix,
        string valueTypeName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.IsDictionary = true;
        builder.IndexerAttributeNamePrefix = attributeNamePrefix;
        builder.IndexerValueTypeName = valueTypeName;
    }

    public static bool IsDirectiveAttribute(this BoundAttributeDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return
            builder.Metadata.TryGetValue(ComponentMetadata.Common.DirectiveAttribute, out var value) &&
            string.Equals(bool.TrueString, value);
    }

    public static void SetPropertyName(this BoundAttributeParameterDescriptorBuilder builder, string propertyName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (propertyName == null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        builder.Metadata[TagHelperMetadata.Common.PropertyName] = propertyName;
    }

    public static string GetPropertyName(this BoundAttributeParameterDescriptorBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Metadata.TryGetValue(TagHelperMetadata.Common.PropertyName, out var value))
        {
            return value;
        }

        return null;
    }
}
