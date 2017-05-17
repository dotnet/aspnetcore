// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCSharpDocument : RazorCSharpDocument
    {
        private readonly string _generatedCode;
        private readonly RazorDiagnostic[] _diagnostics;
        private readonly LineMapping[] _lineMappings;
        private readonly RazorCodeGenerationOptions _options;

        public DefaultRazorCSharpDocument(
            string generatedCode,
            RazorCodeGenerationOptions options,
            RazorDiagnostic[] diagnostics,
            LineMapping[] lineMappings)
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
            _lineMappings = lineMappings ?? Array.Empty<LineMapping>();
        }

        public override IReadOnlyList<RazorDiagnostic> Diagnostics => _diagnostics;

        public override string GeneratedCode => _generatedCode;

        public override IReadOnlyList<LineMapping> LineMappings => _lineMappings;

        public override RazorCodeGenerationOptions Options => _options;
    }
}
