// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorSyntaxTree : RazorSyntaxTree
{
    private readonly IReadOnlyList<RazorDiagnostic> _diagnostics;
    private IReadOnlyList<RazorDiagnostic> _allDiagnostics;

    public DefaultRazorSyntaxTree(
        SyntaxNode root,
        RazorSourceDocument source,
        IReadOnlyList<RazorDiagnostic> diagnostics,
        RazorParserOptions options)
    {
        Root = root;
        Source = source;
        _diagnostics = diagnostics;
        Options = options;
    }

    public override IReadOnlyList<RazorDiagnostic> Diagnostics
    {
        get
        {
            if (_allDiagnostics == null)
            {
                var allDiagnostics = new HashSet<RazorDiagnostic>();
                for (var i = 0; i < _diagnostics.Count; i++)
                {
                    allDiagnostics.Add(_diagnostics[i]);
                }

                var rootDiagnostics = Root.GetAllDiagnostics();
                for (var i = 0; i < rootDiagnostics.Count; i++)
                {
                    allDiagnostics.Add(rootDiagnostics[i]);
                }

                var allOrderedDiagnostics = allDiagnostics.OrderBy(diagnostic => diagnostic.Span.AbsoluteIndex);
                _allDiagnostics = allOrderedDiagnostics.ToArray();
            }

            return _allDiagnostics;
        }
    }

    public override RazorParserOptions Options { get; }

    internal override SyntaxNode Root { get; }

    public override RazorSourceDocument Source { get; }
}
