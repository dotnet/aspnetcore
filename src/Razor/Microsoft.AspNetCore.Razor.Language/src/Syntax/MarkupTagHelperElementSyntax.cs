// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class MarkupTagHelperElementSyntax
{
    private static readonly string TagHelperInfoKey = typeof(TagHelperInfo).Name;

    public TagHelperInfo TagHelperInfo
    {
        get
        {
            var tagHelperInfo = this.GetAnnotationValue(TagHelperInfoKey) as TagHelperInfo;
            return tagHelperInfo;
        }
    }

    public MarkupTagHelperElementSyntax WithTagHelperInfo(TagHelperInfo info)
    {
        var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(TagHelperInfoKey, info)
            };

        var newGreen = Green.WithAnnotationsGreen(annotations.ToArray());

        return (MarkupTagHelperElementSyntax)newGreen.CreateRed(Parent, Position);
    }
}
