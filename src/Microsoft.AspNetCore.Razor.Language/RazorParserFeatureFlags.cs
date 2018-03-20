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

            if (version.CompareTo(RazorLanguageVersion.Version_2_1) >= 0)
            {
                // Added in 2.1
                allowMinimizedBooleanTagHelperAttributes = true;
                allowHtmlCommentsInTagHelpers = true;
            }

            return new DefaultRazorParserFeatureFlags(allowMinimizedBooleanTagHelperAttributes, allowHtmlCommentsInTagHelpers);
        }

        public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

        public abstract bool AllowHtmlCommentsInTagHelpers { get; }

        private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public DefaultRazorParserFeatureFlags(bool allowMinimizedBooleanTagHelperAttributes, bool allowHtmlCommentsInTagHelpers)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
                AllowHtmlCommentsInTagHelpers = allowHtmlCommentsInTagHelpers;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }

            public override bool AllowHtmlCommentsInTagHelpers { get; }
        }
    }
}