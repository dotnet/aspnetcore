// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class RazorConfiguration
    {
        public static readonly RazorConfiguration Default = new RazorConfiguration(
            RazorLanguageVersion.Latest, 
            "unnamed",
            Array.Empty<RazorExtension>(),
            designTime: false);

        // This is used only in some back-compat scenarios. We don't expose it because there's no
        // use case for anyone else to use it.
        internal static readonly RazorConfiguration DefaultDesignTime = new RazorConfiguration(
            RazorLanguageVersion.Latest,
            "unnamed",
            Array.Empty<RazorExtension>(),
            designTime: true);

        public RazorConfiguration(
            RazorLanguageVersion languageVersion, 
            string configurationName,
            IEnumerable<RazorExtension> extensions,
            bool designTime)
        {
            if (languageVersion == null)
            {
                throw new ArgumentNullException(nameof(languageVersion));
            }

            if (configurationName == null)
            {
                throw new ArgumentNullException(nameof(configurationName));
            }

            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            LanguageVersion = languageVersion;
            ConfigurationName = configurationName;
            Extensions = extensions.ToArray();
            DesignTime = designTime;
        }

        public string ConfigurationName { get; }

        public IReadOnlyList<RazorExtension> Extensions { get; }

        public RazorLanguageVersion LanguageVersion { get; }

        public bool DesignTime { get; }
    }
}
