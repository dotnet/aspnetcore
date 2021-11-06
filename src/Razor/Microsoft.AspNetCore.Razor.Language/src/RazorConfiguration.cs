// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorConfiguration : IEquatable<RazorConfiguration>
{
    public static readonly RazorConfiguration Default = new DefaultRazorConfiguration(
        RazorLanguageVersion.Latest,
        "unnamed",
        Array.Empty<RazorExtension>(),
        false);

    public static RazorConfiguration Create(
        RazorLanguageVersion languageVersion,
        string configurationName,
        IEnumerable<RazorExtension> extensions,
        bool useConsolidatedMvcViews = false)
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

        return new DefaultRazorConfiguration(languageVersion, configurationName, extensions.ToArray(), useConsolidatedMvcViews);
    }

    public abstract string ConfigurationName { get; }

    public abstract IReadOnlyList<RazorExtension> Extensions { get; }

    public abstract RazorLanguageVersion LanguageVersion { get; }

    public abstract bool UseConsolidatedMvcViews { get; }

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

        if (UseConsolidatedMvcViews != other.UseConsolidatedMvcViews)
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
        var hash = HashCodeCombiner.Start();
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
            RazorExtension[] extensions,
            bool useConsolidatedMvcViews = false)
        {
            LanguageVersion = languageVersion;
            ConfigurationName = configurationName;
            Extensions = extensions;
            UseConsolidatedMvcViews = useConsolidatedMvcViews;
        }

        public override string ConfigurationName { get; }

        public override IReadOnlyList<RazorExtension> Extensions { get; }

        public override RazorLanguageVersion LanguageVersion { get; }

        public override bool UseConsolidatedMvcViews { get; }
    }
}
