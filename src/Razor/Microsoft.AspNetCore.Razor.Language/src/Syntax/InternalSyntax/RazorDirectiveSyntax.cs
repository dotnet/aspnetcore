// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
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
}
