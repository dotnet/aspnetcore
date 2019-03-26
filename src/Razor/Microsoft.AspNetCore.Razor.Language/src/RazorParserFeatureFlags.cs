// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal abstract class RazorParserFeatureFlags
    {
        public static RazorParserFeatureFlags Create(RazorLanguageVersion version)
        {
            var allowMinimizedBooleanTagHelperAttributes = false;
            var allowHtmlCommentsInTagHelpers = false;
            var allowComponentFileKind = false;
            var allowRazorInAllCodeBlocks = false;
            var allowUsingVariableDeclarations = false;
            var experimental_AllowConditionalDataDashAttributes = false;

            if (version.CompareTo(RazorLanguageVersion.Version_2_1) >= 0)
            {
                // Added in 2.1
                allowMinimizedBooleanTagHelperAttributes = true;
                allowHtmlCommentsInTagHelpers = true;
            }

            if (version.CompareTo(RazorLanguageVersion.Version_3_0) >= 0)
            {
                // Added in 3.0
                allowComponentFileKind = true;
                allowRazorInAllCodeBlocks = true;
                allowUsingVariableDeclarations = true;
            }

            if (version.CompareTo(RazorLanguageVersion.Experimental) >= 0)
            {
                experimental_AllowConditionalDataDashAttributes = true;
            }

            return new DefaultRazorParserFeatureFlags(
                allowMinimizedBooleanTagHelperAttributes,
                allowHtmlCommentsInTagHelpers,
                allowComponentFileKind,
                allowRazorInAllCodeBlocks,
                allowUsingVariableDeclarations,
                experimental_AllowConditionalDataDashAttributes);
        }

        public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

        public abstract bool AllowHtmlCommentsInTagHelpers { get; }

        public abstract bool AllowComponentFileKind { get; }

        public abstract bool AllowRazorInAllCodeBlocks { get; }

        public abstract bool AllowUsingVariableDeclarations { get; }

        public abstract bool EXPERIMENTAL_AllowConditionalDataDashAttributes { get; }

        private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public DefaultRazorParserFeatureFlags(
                bool allowMinimizedBooleanTagHelperAttributes,
                bool allowHtmlCommentsInTagHelpers,
                bool allowComponentFileKind,
                bool allowRazorInAllCodeBlocks,
                bool allowUsingVariableDeclarations,
                bool experimental_AllowConditionalDataDashAttributes)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelpers;
                AllowComponentFileKind = allowComponentFileKind;
                AllowRazorInAllCodeBlocks = allowRazorInAllCodeBlocks;
                AllowUsingVariableDeclarations = allowUsingVariableDeclarations;
                EXPERIMENTAL_AllowConditionalDataDashAttributes = experimental_AllowConditionalDataDashAttributes;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }

            public override bool AllowComponentFileKind { get; }

            public override bool AllowRazorInAllCodeBlocks { get; }

            public override bool AllowUsingVariableDeclarations { get; }

            public override bool EXPERIMENTAL_AllowConditionalDataDashAttributes { get; }
        }
    }
}
