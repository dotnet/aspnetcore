// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

internal sealed partial class RazorDirectiveSyntax
{
    private static readonly string DirectiveDescriptorKey = typeof(DirectiveDescriptor).Name;

    public DirectiveDescriptor DirectiveDescriptor
    {
        get
        {
            var annotation = GetAnnotations().FirstOrDefault(n => n.Kind == DirectiveDescriptorKey);
            return annotation?.Data as DirectiveDescriptor;
        }
    }

    public RazorDirectiveSyntax WithDirectiveDescriptor(DirectiveDescriptor descriptor)
    {
        var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(DirectiveDescriptorKey, descriptor)
            };

        var newGreen = this.WithAnnotationsGreen(annotations.ToArray());

        return newGreen;
    }
}
