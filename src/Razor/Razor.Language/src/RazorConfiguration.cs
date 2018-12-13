// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorConfiguration
    {
        public static readonly RazorConfiguration Default = new DefaultRazorConfiguration(
            RazorLanguageVersion.Latest, 
            "unnamed",
            Array.Empty<RazorExtension>());

        public static RazorConfiguration Create(
            RazorLanguageVersion languageVersion,
            string configurationName,
            IEnumerable<RazorExtension> extensions)
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

            return new DefaultRazorConfiguration(languageVersion, configurationName, extensions.ToArray());
        }

        public abstract string ConfigurationName { get; }

        public abstract IReadOnlyList<RazorExtension> Extensions { get; }

        public abstract RazorLanguageVersion LanguageVersion { get; }

        private class DefaultRazorConfiguration : RazorConfiguration
        {
            public DefaultRazorConfiguration(
                RazorLanguageVersion languageVersion,
                string configurationName,
                RazorExtension[] extensions)
            {
                LanguageVersion = languageVersion;
                ConfigurationName = configurationName;
                Extensions = extensions;
            }

            public override string ConfigurationName { get; }

            public override IReadOnlyList<RazorExtension> Extensions { get; }

            public override RazorLanguageVersion LanguageVersion { get; }
        }
    }
}
