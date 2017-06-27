// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RequiredAttributeDescriptorBuilder
    {
        public abstract RequiredAttributeDescriptorBuilder Name(string name);

        public abstract RequiredAttributeDescriptorBuilder NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode nameComparison);

        public abstract RequiredAttributeDescriptorBuilder Value(string value);

        public abstract RequiredAttributeDescriptorBuilder ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode valueComparison);

        public abstract RequiredAttributeDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic);
    }
}
