// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public enum RazorLanguageVersion
    {
        Version1_0 = 1,

        Version1_1 = 2,

        Version2_0 = 3,

        Version2_1 = 4,
    }

    internal static class RazorLanguageVersionExtensions
    {
        internal static bool IsValid(this RazorLanguageVersion version)
        {
            switch (version)
            {
                case RazorLanguageVersion.Version1_0:
                case RazorLanguageVersion.Version1_1:
                case RazorLanguageVersion.Version2_0:
                case RazorLanguageVersion.Version2_1:
                    return true;
            }

            return false;
        }
    }
}
