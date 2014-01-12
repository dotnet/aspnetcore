// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Razor.Parser
{
    public static class SyntaxConstants
    {
        public static readonly string TextTagName = "text";
        public static readonly char TransitionCharacter = '@';
        public static readonly string TransitionString = "@";
        public static readonly string StartCommentSequence = "@*";
        public static readonly string EndCommentSequence = "*@";

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Class is nested to provide better organization")]
        [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "This type name should not cause a conflict")]
        public static class CSharp
        {
            public static readonly int UsingKeywordLength = 5;
            public static readonly string InheritsKeyword = "inherits";
            public static readonly string FunctionsKeyword = "functions";
            public static readonly string SectionKeyword = "section";
            public static readonly string HelperKeyword = "helper";
            public static readonly string ElseIfKeyword = "else if";
            public static readonly string NamespaceKeyword = "namespace";
            public static readonly string ClassKeyword = "class";
            public static readonly string LayoutKeyword = "layout";
            public static readonly string SessionStateKeyword = "sessionstate";
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Class is nested to provide better organization")]
        public static class VB
        {
            public static readonly int ImportsKeywordLength = 7;
            public static readonly string EndKeyword = "End";
            public static readonly string CodeKeyword = "Code";
            public static readonly string FunctionsKeyword = "Functions";
            public static readonly string SectionKeyword = "Section";
            public static readonly string StrictKeyword = "Strict";
            public static readonly string ExplicitKeyword = "Explicit";
            public static readonly string OffKeyword = "Off";
            public static readonly string HelperKeyword = "Helper";
            public static readonly string SelectCaseKeyword = "Select Case";
            public static readonly string LayoutKeyword = "Layout";
            public static readonly string EndCodeKeyword = "End Code";
            public static readonly string EndHelperKeyword = "End Helper";
            public static readonly string EndFunctionsKeyword = "End Functions";
            public static readonly string EndSectionKeyword = "End Section";
            public static readonly string SessionStateKeyword = "SessionState";
        }
    }
}
