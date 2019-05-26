// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TagHelperAttributeInfo
    {
        public TagHelperAttributeInfo(
            string name,
            string parameterName,
            AttributeStructure attributeStructure,
            bool bound,
            bool isDirectiveAttribute)
        {
            Name = name;
            ParameterName = parameterName;
            AttributeStructure = attributeStructure;
            Bound = bound;
            IsDirectiveAttribute = isDirectiveAttribute;
        }

        public string Name { get; }

        public string ParameterName { get; }

        public AttributeStructure AttributeStructure { get; }

        public bool Bound { get; }

        public bool IsDirectiveAttribute { get; }
    }
}
