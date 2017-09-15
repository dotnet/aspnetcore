// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal abstract class RazorParserFeatureFlags
    {
        public static RazorParserFeatureFlags Create(RazorLanguageVersion version)
        {
            if (!version.IsValid())
            {
                throw new ArgumentException(Resources.FormatInvalidRazorLanguageVersion(version.ToString()));
            }

            var allowMinimizedBooleanTagHelperAttributes = false;

            if (version == RazorLanguageVersion.Version2_1)
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
