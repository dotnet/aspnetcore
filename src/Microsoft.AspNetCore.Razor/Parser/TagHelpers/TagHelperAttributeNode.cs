// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Parser.TagHelpers
{
    public class TagHelperAttributeNode
    {
        public TagHelperAttributeNode(string name, SyntaxTreeNode value, HtmlAttributeValueStyle valueStyle)
        {
            Name = name;
            Value = value;
            ValueStyle = valueStyle;
        }

        // Internal for testing
        internal TagHelperAttributeNode(string name, SyntaxTreeNode value)
            : this(name, value, HtmlAttributeValueStyle.DoubleQuotes)
        {
        }

        public string Name { get; }

        public SyntaxTreeNode Value { get; }

        public HtmlAttributeValueStyle ValueStyle { get; }
    }
}
