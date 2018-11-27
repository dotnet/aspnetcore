// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal sealed partial class RazorDirectiveSyntax
    {
        private static readonly string DirectiveDescriptorKey = typeof(DirectiveDescriptor).Name;

        public DirectiveDescriptor DirectiveDescriptor
        {
            get
            {
                var descriptor = this.GetAnnotationValue(DirectiveDescriptorKey) as DirectiveDescriptor;
                return descriptor;
            }
        }

        public RazorDirectiveSyntax WithDirectiveDescriptor(DirectiveDescriptor descriptor)
        {
            var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(DirectiveDescriptorKey, descriptor)
            };

            var newGreen = Green.WithAnnotationsGreen(annotations.ToArray());

            return (RazorDirectiveSyntax)newGreen.CreateRed(Parent, Position);
        }
    }
}
