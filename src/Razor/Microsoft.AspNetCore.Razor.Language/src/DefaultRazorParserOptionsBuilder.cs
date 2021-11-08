// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorParserOptionsBuilder : RazorParserOptionsBuilder
{
    private bool _designTime;

    public DefaultRazorParserOptionsBuilder(RazorConfiguration configuration, string fileKind)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Configuration = configuration;
        LanguageVersion = configuration.LanguageVersion;
        FileKind = fileKind;
    }

    public DefaultRazorParserOptionsBuilder(bool designTime, RazorLanguageVersion version, string fileKind)
    {
        _designTime = designTime;
        LanguageVersion = version;
        FileKind = fileKind;
    }

    public override RazorConfiguration Configuration { get; }

    public override bool DesignTime => _designTime;

    public override ICollection<DirectiveDescriptor> Directives { get; } = new List<DirectiveDescriptor>();

    public override string FileKind { get; }

    public override bool ParseLeadingDirectives { get; set; }

    public override RazorLanguageVersion LanguageVersion { get; }

    public override RazorParserOptions Build()
    {
        return new DefaultRazorParserOptions(Directives.ToArray(), DesignTime, ParseLeadingDirectives, LanguageVersion, FileKind ?? FileKinds.Legacy);
    }

    public override void SetDesignTime(bool designTime)
    {
        _designTime = designTime;
    }
}
