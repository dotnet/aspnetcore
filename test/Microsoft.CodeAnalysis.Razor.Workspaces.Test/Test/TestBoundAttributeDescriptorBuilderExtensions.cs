// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public static class TestBoundAttributeDescriptorBuilderExtensions
    {
        public static BoundAttributeDescriptorBuilder Name(this BoundAttributeDescriptorBuilder builder, string name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Name = name;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder TypeName(this BoundAttributeDescriptorBuilder builder, string typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.TypeName = typeName;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder PropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.SetPropertyName(propertyName);

            return builder;
        }

        public static BoundAttributeDescriptorBuilder DisplayName(this BoundAttributeDescriptorBuilder builder, string displayName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.DisplayName = displayName;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder AsEnum(this BoundAttributeDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.IsEnum = true;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder AsDictionaryAttribute(
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

            return builder;
        }

        public static BoundAttributeDescriptorBuilder Documentation(this BoundAttributeDescriptorBuilder builder, string documentation)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Documentation = documentation;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder AddMetadata(this BoundAttributeDescriptorBuilder builder, string key, string value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Metadata[key] = value;

            return builder;
        }

        public static BoundAttributeDescriptorBuilder AddDiagnostic(this BoundAttributeDescriptorBuilder builder, RazorDiagnostic diagnostic)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Diagnostics.Add(diagnostic);

            return builder;
        }
    }
}
