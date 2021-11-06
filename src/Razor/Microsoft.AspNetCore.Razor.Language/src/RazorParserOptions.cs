// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorParserOptions
{
    public static RazorParserOptions CreateDefault()
    {
        return new DefaultRazorParserOptions(
            Array.Empty<DirectiveDescriptor>(),
            designTime: false,
            parseLeadingDirectives: false,
            version: RazorLanguageVersion.Latest,
            fileKind: FileKinds.Legacy);
    }

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure)
    {
        return Create(configure, fileKind: FileKinds.Legacy);
    }

    public static RazorParserOptions Create(Action<RazorParserOptionsBuilder> configure, string fileKind)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DefaultRazorParserOptionsBuilder(designTime: false, version: RazorLanguageVersion.Latest, fileKind ?? FileKinds.Legacy);
        configure(builder);
        var options = builder.Build();

        return options;
    }

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure)
    {
        return CreateDesignTime(configure, fileKind: null);
    }

    public static RazorParserOptions CreateDesignTime(Action<RazorParserOptionsBuilder> configure, string fileKind)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DefaultRazorParserOptionsBuilder(designTime: true, version: RazorLanguageVersion.Latest, fileKind ?? FileKinds.Legacy);
        configure(builder);
        var options = builder.Build();

        return options;
    }

    public abstract bool DesignTime { get; }

    public abstract IReadOnlyCollection<DirectiveDescriptor> Directives { get; }

    /// <summary>
    /// Gets a value which indicates whether the parser will parse only the leading directives. If <c>true</c>
    /// the parser will halt at the first HTML content or C# code block. If <c>false</c> the whole document is parsed.
    /// </summary>
    /// <remarks>
    /// Currently setting this option to <c>true</c> will result in only the first line of directives being parsed.
    /// In a future release this may be updated to include all leading directive content.
    /// </remarks>
    public abstract bool ParseLeadingDirectives { get; }

    public virtual RazorLanguageVersion Version { get; } = RazorLanguageVersion.Latest;

    internal virtual string FileKind { get; }

    internal virtual RazorParserFeatureFlags FeatureFlags { get; }
}
