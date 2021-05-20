// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultBoundAttributeParameterDescriptor : BoundAttributeParameterDescriptor
    {
        public DefaultBoundAttributeParameterDescriptor(
            string kind,
            string name,
            string typeName,
            bool isEnum,
            string documentation,
            string displayName,
            bool caseSensitive,
            Dictionary<string, string> metadata,
            RazorDiagnostic[] diagnostics)
            : base(kind)
        {
            Name = name;
            TypeName = typeName;
            IsEnum = isEnum;
            Documentation = documentation;
            DisplayName = displayName;
            CaseSensitive = caseSensitive;

            Metadata = metadata;
            Diagnostics = diagnostics;

            IsStringProperty = typeName == typeof(string).FullName || typeName == "string";
            IsBooleanProperty = typeName == typeof(bool).FullName || typeName == "bool";
        }
    }
}
