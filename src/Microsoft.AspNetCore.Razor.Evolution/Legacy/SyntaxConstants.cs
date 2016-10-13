// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal static class SyntaxConstants
    {
        public static readonly string TextTagName = "text";
        public static readonly char TransitionCharacter = '@';
        public static readonly string TransitionString = "@";
        public static readonly string StartCommentSequence = "@*";
        public static readonly string EndCommentSequence = "*@";

        public static class CSharp
        {
            public static readonly int UsingKeywordLength = 5;
            public static readonly string TagHelperPrefixKeyword = "tagHelperPrefix";
            public static readonly string AddTagHelperKeyword = "addTagHelper";
            public static readonly string RemoveTagHelperKeyword = "removeTagHelper";
            public static readonly string InheritsKeyword = "inherits";
            public static readonly string FunctionsKeyword = "functions";
            public static readonly string SectionKeyword = "section";
            public static readonly string ElseIfKeyword = "else if";
            public static readonly string NamespaceKeyword = "namespace";
            public static readonly string ClassKeyword = "class";

            // Not supported. Only used for error cases.
            public static readonly string HelperKeyword = "helper";
        }
    }
}
