// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorCSharpDocument : RazorCSharpDocument
{
    private readonly string _generatedCode;
    private readonly RazorDiagnostic[] _diagnostics;
    private readonly SourceMapping[] _sourceMappings;
    private readonly LinePragma[] _linePragmas;
    private readonly RazorCodeGenerationOptions _options;

    public DefaultRazorCSharpDocument(
        string generatedCode,
        RazorCodeGenerationOptions options,
        RazorDiagnostic[] diagnostics,
        SourceMapping[] sourceMappings,
        LinePragma[] linePragmas)
    {
        if (generatedCode == null)
        {
            throw new ArgumentNullException(nameof(generatedCode));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _generatedCode = generatedCode;
        _options = options;

        _diagnostics = diagnostics ?? Array.Empty<RazorDiagnostic>();
        _sourceMappings = sourceMappings ?? Array.Empty<SourceMapping>();
        _linePragmas = linePragmas ?? Array.Empty<LinePragma>();
    }

    public override IReadOnlyList<RazorDiagnostic> Diagnostics => _diagnostics;

    public override string GeneratedCode => _generatedCode;

    public override IReadOnlyList<SourceMapping> SourceMappings => _sourceMappings;

    internal override IReadOnlyList<LinePragma> LinePragmas => _linePragmas;

    public override RazorCodeGenerationOptions Options => _options;
}
