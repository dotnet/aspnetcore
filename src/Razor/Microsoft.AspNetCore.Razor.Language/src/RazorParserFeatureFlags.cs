// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal abstract class RazorParserFeatureFlags
    {
        public static RazorParserFeatureFlags Create(RazorLanguageVersion version, string fileKind)
        {
            if (fileKind == null)
            {
                throw new ArgumentNullException(nameof(fileKind));
            }

            var allowMinimizedBooleanTagHelperAttributes = false;
            var allowHtmlCommentsInTagHelpers = false;
            var allowComponentFileKind = false;
            var allowRazorInAllCodeBlocks = false;
            var allowUsingVariableDeclarations = false;
            var allowConditionalDataDashAttributes = false;

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

            if (FileKinds.IsComponent(fileKind))
            {
                allowConditionalDataDashAttributes = true;
            }

            if (version.CompareTo(RazorLanguageVersion.Experimental) >= 0)
            {
                allowConditionalDataDashAttributes = true;
            }

            return new DefaultRazorParserFeatureFlags(
                allowMinimizedBooleanTagHelperAttributes,
                allowHtmlCommentsInTagHelpers,
                allowComponentFileKind,
                allowRazorInAllCodeBlocks,
                allowUsingVariableDeclarations,
                allowConditionalDataDashAttributes);
        }

        public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

        public abstract bool AllowHtmlCommentsInTagHelpers { get; }

        public abstract bool AllowComponentFileKind { get; }

        public abstract bool AllowRazorInAllCodeBlocks { get; }

        public abstract bool AllowUsingVariableDeclarations { get; }

        public abstract bool AllowConditionalDataDashAttributes { get; }

        private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public DefaultRazorParserFeatureFlags(
                bool allowMinimizedBooleanTagHelperAttributes,
                bool allowHtmlCommentsInTagHelpers,
                bool allowComponentFileKind,
                bool allowRazorInAllCodeBlocks,
                bool allowUsingVariableDeclarations,
                bool allowConditionalDataDashAttributesInComponents)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelpers;
                AllowComponentFileKind = allowComponentFileKind;
                AllowRazorInAllCodeBlocks = allowRazorInAllCodeBlocks;
                AllowUsingVariableDeclarations = allowUsingVariableDeclarations;
                AllowConditionalDataDashAttributes = allowConditionalDataDashAttributesInComponents;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }

            public override bool AllowComponentFileKind { get; }

            public override bool AllowRazorInAllCodeBlocks { get; }

            public override bool AllowUsingVariableDeclarations { get; }

            public override bool AllowConditionalDataDashAttributes { get; }
        }
    }
}
