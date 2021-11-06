// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorParserOptions : RazorParserOptions
{
    public DefaultRazorParserOptions(DirectiveDescriptor[] directives, bool designTime, bool parseLeadingDirectives, RazorLanguageVersion version, string fileKind)
    {
        if (directives == null)
        {
            throw new ArgumentNullException(nameof(directives));
        }

        Directives = directives;
        DesignTime = designTime;
        ParseLeadingDirectives = parseLeadingDirectives;
        Version = version;
        FeatureFlags = RazorParserFeatureFlags.Create(Version, fileKind);
        FileKind = fileKind;
    }

    public override bool DesignTime { get; }

    public override IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

    public override bool ParseLeadingDirectives { get; }

    public override RazorLanguageVersion Version { get; }

    internal override string FileKind { get; }

    internal override RazorParserFeatureFlags FeatureFlags { get; }
}
