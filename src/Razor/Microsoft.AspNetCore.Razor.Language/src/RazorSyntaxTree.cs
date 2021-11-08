// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorSyntaxTree
{
    internal static RazorSyntaxTree Create(
        SyntaxNode root,
        RazorSourceDocument source,
        IEnumerable<RazorDiagnostic> diagnostics,
        RazorParserOptions options)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new DefaultRazorSyntaxTree(root, source, new List<RazorDiagnostic>(diagnostics), options);
    }

    public static RazorSyntaxTree Parse(RazorSourceDocument source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Parse(source, options: null);
    }

    public static RazorSyntaxTree Parse(RazorSourceDocument source, RazorParserOptions options)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var parser = new RazorParser(options ?? RazorParserOptions.CreateDefault());
        return parser.Parse(source);
    }

    public abstract IReadOnlyList<RazorDiagnostic> Diagnostics { get; }

    public abstract RazorParserOptions Options { get; }

    internal abstract SyntaxNode Root { get; }

    public abstract RazorSourceDocument Source { get; }
}
