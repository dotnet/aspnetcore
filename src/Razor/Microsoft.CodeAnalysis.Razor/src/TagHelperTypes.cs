// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor;

internal static class TagHelperTypes
{
    public const string ITagHelper = "Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper";

    public const string IComponent = "Microsoft.AspNetCore.Components.IComponent";

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
