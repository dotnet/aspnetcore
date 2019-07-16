// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRequiredAttributeDescriptor : RequiredAttributeDescriptor
    {
        public DefaultRequiredAttributeDescriptor(
            string name,
            NameComparisonMode nameComparison,
            bool caseSensitive,
            string value,
            ValueComparisonMode valueComparison,
            string displayName,
            RazorDiagnostic[] diagnostics,
            Dictionary<string, string> metadata)
        {
            Name = name;
            NameComparison = nameComparison;
            CaseSensitive = caseSensitive;
            Value = value;
            ValueComparison = valueComparison;
            DisplayName = displayName;
            Diagnostics = diagnostics;
            Metadata = metadata;
        }
    }
}
