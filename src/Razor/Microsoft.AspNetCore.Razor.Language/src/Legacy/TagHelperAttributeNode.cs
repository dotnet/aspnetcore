// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class TagHelperAttributeNode
{
    public TagHelperAttributeNode(string name, SyntaxNode value, AttributeStructure attributeStructure)
    {
        Name = name;
        Value = value;
        AttributeStructure = attributeStructure;
    }

    // Internal for testing
    internal TagHelperAttributeNode(string name, SyntaxNode value)
        : this(name, value, AttributeStructure.DoubleQuotes)
    {
    }

    public string Name { get; }

    public SyntaxNode Value { get; }

    public AttributeStructure AttributeStructure { get; }
}
