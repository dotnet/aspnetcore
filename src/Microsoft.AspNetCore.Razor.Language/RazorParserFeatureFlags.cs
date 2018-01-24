// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    internal abstract class RazorParserFeatureFlags
    {
        public static RazorParserFeatureFlags Create(RazorLanguageVersion version)
        {
            var allowMinimizedBooleanTagHelperAttributes = false;

            if (version == RazorLanguageVersion.Version_2_1)
            {
                allowMinimizedBooleanTagHelperAttributes = true;
            }

            return new DefaultRazorParserFeatureFlags(allowMinimizedBooleanTagHelperAttributes);
        }

        public abstract bool AllowMinimizedBooleanTagHelperAttributes { get; }

        private class DefaultRazorParserFeatureFlags : RazorParserFeatureFlags
        {
            public DefaultRazorParserFeatureFlags(bool allowMinimizedBooleanTagHelperAttributes)
            {
                AllowMinimizedBooleanTagHelperAttributes = allowMinimizedBooleanTagHelperAttributes;
            }

            public override bool AllowMinimizedBooleanTagHelperAttributes { get; }
        }
    }
}
