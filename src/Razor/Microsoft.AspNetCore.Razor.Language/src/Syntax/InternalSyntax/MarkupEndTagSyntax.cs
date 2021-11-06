// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal sealed partial class MarkupEndTagSyntax
{
    private const string MarkupTransitionKey = "MarkupTransition";

    public bool IsMarkupTransition
    {
        get
        {
            var annotation = GetAnnotations().FirstOrDefault(n => n.Kind == MarkupTransitionKey);
            return annotation != null;
        }
    }

    public MarkupEndTagSyntax AsMarkupTransition()
    {
        var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(MarkupTransitionKey, new object())
            };

        var newGreen = this.WithAnnotationsGreen(annotations.ToArray());

        return newGreen;
    }
}
