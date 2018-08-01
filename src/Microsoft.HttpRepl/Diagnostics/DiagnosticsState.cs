// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.HttpRepl.Diagnostics
{
    public class DiagnosticsState
    {
        public string DiagnosticsEndpoint { get; set; }

        public IReadOnlyList<DiagItem> DiagnosticItems { get; internal set; }

        public IDirectoryStructure DiagEndpointsStructure { get; set; }
    }
}
