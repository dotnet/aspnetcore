// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
    }
}
