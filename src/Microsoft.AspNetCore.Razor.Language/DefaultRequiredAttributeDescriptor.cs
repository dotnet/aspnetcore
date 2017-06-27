// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRequiredAttributeDescriptor : RequiredAttributeDescriptor
    {
        public DefaultRequiredAttributeDescriptor(
            string name,
            NameComparisonMode nameComparison,
            string value,
            ValueComparisonMode valueComparison,
            string displayName,
            RazorDiagnostic[] diagnostics)
        {
            Name = name;
            NameComparison = nameComparison;
            Value = value;
            ValueComparison = valueComparison;
            DisplayName = displayName;
            Diagnostics = diagnostics;
        }
    }
}
