// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorConfiguration : IEquatable<RazorConfiguration>
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

        public override bool Equals(object obj)
        {
            return base.Equals(obj as RazorConfiguration);
        }

        public virtual bool Equals(RazorConfiguration other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (LanguageVersion != other.LanguageVersion)
            {
                return false;
            }

            if (ConfigurationName != other.ConfigurationName)
            {
                return false;
            }

            if (Extensions.Count != other.Extensions.Count)
            {
                return false;
            }

            for (var i = 0; i < Extensions.Count; i++)
            {
                if (Extensions[i].ExtensionName != other.Extensions[i].ExtensionName)
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(LanguageVersion);
            hash.Add(ConfigurationName);

            for (var i = 0; i < Extensions.Count; i++)
            {
                hash.Add(Extensions[i].ExtensionName);
            }

            return hash;
        }

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
