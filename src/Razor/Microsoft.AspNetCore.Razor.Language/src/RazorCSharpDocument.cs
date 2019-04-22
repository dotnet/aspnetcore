// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorCSharpDocument
    {
        public abstract string GeneratedCode { get; }

        public abstract IReadOnlyList<SourceMapping> SourceMappings { get; }

        public abstract IReadOnlyList<RazorDiagnostic> Diagnostics { get; }

        public abstract RazorCodeGenerationOptions Options { get; }

        internal virtual IReadOnlyList<LinePragma> LinePragmas { get; }

        public static RazorCSharpDocument Create(string generatedCode, RazorCodeGenerationOptions options, IEnumerable<RazorDiagnostic> diagnostics)
        {
            if (generatedCode == null)
            {
                throw new ArgumentNullException(nameof(generatedCode));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            return new DefaultRazorCSharpDocument(generatedCode, options, diagnostics.ToArray(), sourceMappings: null, linePragmas: null);
        }

        public static RazorCSharpDocument Create(
            string generatedCode,
            RazorCodeGenerationOptions options,
            IEnumerable<RazorDiagnostic> diagnostics,
            IEnumerable<SourceMapping> sourceMappings,
            IEnumerable<LinePragma> linePragmas)
        {
            if (generatedCode == null)
            {
                throw new ArgumentNullException(nameof(generatedCode));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (sourceMappings == null)
            {
                throw new ArgumentNullException(nameof(sourceMappings));
            }

            return new DefaultRazorCSharpDocument(generatedCode, options, diagnostics.ToArray(), sourceMappings.ToArray(), linePragmas.ToArray());
        }
    }
}
