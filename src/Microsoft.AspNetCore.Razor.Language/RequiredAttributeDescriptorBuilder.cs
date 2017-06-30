// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RequiredAttributeDescriptorBuilder
    {
        public abstract string Name { get; set; }

        public abstract RequiredAttributeDescriptor.NameComparisonMode NameComparisonMode { get; set; }

        public abstract string Value { get; set; }

        public abstract RequiredAttributeDescriptor.ValueComparisonMode ValueComparisonMode { get; set; }

        public abstract RazorDiagnosticCollection Diagnostics { get; }
    }
}
