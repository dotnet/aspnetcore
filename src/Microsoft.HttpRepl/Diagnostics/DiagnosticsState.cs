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
