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
            }

            if (version.CompareTo(RazorLanguageVersion.Experimental) >= 0)
            {
                experimental_AllowConditionalDataDashAttributes = true;
            }

            return new DefaultRazorParserFeatureFlags(
                allowMinimizedBooleanTagHelperAttributes,
                allowHtmlCommentsInTagHelpers,
                allowComponentFileKind,
                experimental_AllowConditionalDataDashAttributes);
        }

        public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

        public abstract bool AllowHtmlCommentsInTagHelpers { get; }

        public abstract bool AllowComponentFileKind { get; }

        public abstract bool EXPERIMENTAL_AllowConditionalDataDashAttributes { get; }

        private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public DefaultRazorParserFeatureFlags(
                bool allowMinimizedBooleanTagHelperAttributes,
                bool allowHtmlCommentsInTagHelpers,
                bool allowComponentFileKind,
                bool experimental_AllowConditionalDataDashAttributes)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelpers;
                AllowComponentFileKind = allowComponentFileKind;
                EXPERIMENTAL_AllowConditionalDataDashAttributes = experimental_AllowConditionalDataDashAttributes;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }

            public override bool AllowComponentFileKind { get; }

            public override bool EXPERIMENTAL_AllowConditionalDataDashAttributes { get; }
        }
    }
}
