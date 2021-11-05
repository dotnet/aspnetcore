// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal static class SyntaxConstants
{
    public const string TextTagName = "text";
    public const char TransitionCharacter = '@';
    public const string TransitionString = "@";
    public const string StartCommentSequence = "@*";
    public const string EndCommentSequence = "*@";
    public const string SpanContextKind = "SpanData";

    public static class CSharp
    {
        public const int UsingKeywordLength = 5;
        public const string TagHelperPrefixKeyword = "tagHelperPrefix";
        public const string AddTagHelperKeyword = "addTagHelper";
        public const string RemoveTagHelperKeyword = "removeTagHelper";
        public const string InheritsKeyword = "inherits";
        public const string FunctionsKeyword = "functions";
        public const string SectionKeyword = "section";
        public const string ElseIfKeyword = "else if";
        public const string NamespaceKeyword = "namespace";
        public const string ClassKeyword = "class";

        // Not supported. Only used for error cases.
        public const string HelperKeyword = "helper";
    }
}
