// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorCSharpDocument
    {
        public string GeneratedCode { get; set; }

        internal IReadOnlyList<LineMapping> LineMappings { get; set; }

        public IReadOnlyList<RazorError> Diagnostics { get; set; }
    }
}
