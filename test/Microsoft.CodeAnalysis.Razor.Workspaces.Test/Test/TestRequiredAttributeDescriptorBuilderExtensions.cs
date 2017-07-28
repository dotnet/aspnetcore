// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public static class TestRequiredAttributeDescriptorBuilderExtensions
    {
        public static RequiredAttributeDescriptorBuilder Name(this RequiredAttributeDescriptorBuilder builder, string name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Name = name;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder NameComparisonMode(
            this RequiredAttributeDescriptorBuilder builder,
            RequiredAttributeDescriptor.NameComparisonMode nameComparison)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.NameComparisonMode = nameComparison;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder Value(this RequiredAttributeDescriptorBuilder builder, string value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Value = value;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder ValueComparisonMode(
            this RequiredAttributeDescriptorBuilder builder,
            RequiredAttributeDescriptor.ValueComparisonMode valueComparison)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ValueComparisonMode = valueComparison;

            return builder;
        }

        public static RequiredAttributeDescriptorBuilder AddDiagnostic(this RequiredAttributeDescriptorBuilder builder, RazorDiagnostic diagnostic)
        {
            builder.Diagnostics.Add(diagnostic);

            return builder;
        }
    }
}
