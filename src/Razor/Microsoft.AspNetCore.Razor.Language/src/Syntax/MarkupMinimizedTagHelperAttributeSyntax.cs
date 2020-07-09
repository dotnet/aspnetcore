// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal sealed partial class MarkupMinimizedTagHelperAttributeSyntax
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

        public MarkupMinimizedTagHelperAttributeSyntax WithTagHelperAttributeInfo(TagHelperAttributeInfo info)
        {
            var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(TagHelperAttributeInfoKey, info)
            };

            var newGreen = Green.WithAnnotationsGreen(annotations.ToArray());

            return (MarkupMinimizedTagHelperAttributeSyntax)newGreen.CreateRed(Parent, Position);
        }
    }
}
