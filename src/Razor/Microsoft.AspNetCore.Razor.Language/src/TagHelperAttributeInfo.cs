// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

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
