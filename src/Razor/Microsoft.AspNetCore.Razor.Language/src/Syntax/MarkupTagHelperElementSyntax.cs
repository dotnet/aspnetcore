// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
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
}
