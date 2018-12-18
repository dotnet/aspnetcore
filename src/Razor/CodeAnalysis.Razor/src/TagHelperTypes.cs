// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TagHelperTypes
    {
        public const string ITagHelper = "Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper";

        public const string IDictionary = "System.Collections.Generic.IDictionary`2";

        public const string HtmlAttributeNameAttribute = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute";

        public const string HtmlAttributeNotBoundAttribute = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute";

        public const string HtmlTargetElementAttribute = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute";

        public const string OutputElementHintAttribute = "Microsoft.AspNetCore.Razor.TagHelpers.OutputElementHintAttribute";

        public const string RestrictChildrenAttribute = "Microsoft.AspNetCore.Razor.TagHelpers.RestrictChildrenAttribute";

        public static class HtmlAttributeName
        {
            public const string Name = "Name";
            public const string DictionaryAttributePrefix = "DictionaryAttributePrefix";
        }

        public static class HtmlTargetElement
        {
            public const string Attributes = "Attributes";

            public const string ParentTag = "ParentTag";

            public const string TagStructure = "TagStructure";
        }
    }
}
