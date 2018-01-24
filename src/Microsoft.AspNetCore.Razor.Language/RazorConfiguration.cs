// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class RazorConfiguration
    {
        public static readonly RazorConfiguration DefaultRuntime = new RazorConfiguration(RazorLanguageVersion.Latest, designTime: false);
        public static readonly RazorConfiguration DefaultDesignTime = new RazorConfiguration(RazorLanguageVersion.Latest, designTime: true);

        public RazorConfiguration(RazorLanguageVersion languageVersion, bool designTime)
        {
            if (languageVersion == null)
            {
                throw new ArgumentNullException(nameof(languageVersion));
            }

            LanguageVersion = languageVersion;
            DesignTime = designTime;
        }

        public RazorLanguageVersion LanguageVersion { get; }

        public bool DesignTime { get; }
    }
}
