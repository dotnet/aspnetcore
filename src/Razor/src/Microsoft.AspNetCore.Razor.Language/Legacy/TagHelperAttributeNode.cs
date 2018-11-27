// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperAttributeNode
    {
        public TagHelperAttributeNode(string name, SyntaxTreeNode value, AttributeStructure attributeStructure)
        {
            Name = name;
            Value = value;
            AttributeStructure = attributeStructure;
        }

        // Internal for testing
        internal TagHelperAttributeNode(string name, SyntaxTreeNode value)
            : this(name, value, AttributeStructure.DoubleQuotes)
        {
        }

        public string Name { get; }

        public SyntaxTreeNode Value { get; }

        public AttributeStructure AttributeStructure { get; }
    }
}
