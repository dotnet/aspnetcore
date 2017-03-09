// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorCSharpDocument
    {
        public string GeneratedCode { get; set; }

        public IReadOnlyList<LineMapping> LineMappings { get; set; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; set; }
    }
}
