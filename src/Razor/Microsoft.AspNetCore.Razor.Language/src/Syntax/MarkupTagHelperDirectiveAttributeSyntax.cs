// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class MarkupTagHelperDirectiveAttributeSyntax
{
    private static readonly string TagHelperAttributeInfoKey = typeof(TagHelperAttributeInfo).Name;

    public TagHelperAttributeInfo TagHelperAttributeInfo
    {
        get
        {
            var tagHelperAttributeInfo = this.GetAnnotationValue(TagHelperAttributeInfoKey) as TagHelperAttributeInfo;
            return tagHelperAttributeInfo;
        }
    }

    public string FullName
    {
        get
        {
            var fullName = string.Concat(
                Transition.GetContent(),
                Name.GetContent(),
                Colon?.GetContent() ?? string.Empty,
                ParameterName?.GetContent() ?? string.Empty);
            return fullName;
        }
    }

    public MarkupTagHelperDirectiveAttributeSyntax WithTagHelperAttributeInfo(TagHelperAttributeInfo info)
    {
        var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(TagHelperAttributeInfoKey, info)
            };

        var newGreen = Green.WithAnnotationsGreen(annotations.ToArray());

        return (MarkupTagHelperDirectiveAttributeSyntax)newGreen.CreateRed(Parent, Position);
    }
}
