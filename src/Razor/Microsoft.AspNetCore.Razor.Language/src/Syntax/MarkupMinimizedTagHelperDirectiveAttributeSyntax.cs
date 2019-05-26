// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal sealed partial class MarkupMinimizedTagHelperDirectiveAttributeSyntax
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

        public MarkupMinimizedTagHelperDirectiveAttributeSyntax WithTagHelperAttributeInfo(TagHelperAttributeInfo info)
        {
            var annotations = new List<SyntaxAnnotation>(GetAnnotations())
            {
                new SyntaxAnnotation(TagHelperAttributeInfoKey, info)
            };

            var newGreen = Green.WithAnnotationsGreen(annotations.ToArray());

            return (MarkupMinimizedTagHelperDirectiveAttributeSyntax)newGreen.CreateRed(Parent, Position);
        }
    }
}
