// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers
{
    public class StartupAnalyzerTest : StartupAnalyzerTestBase
    {
        public StartupAnalyzerTest()
        {
            Runner = new AnalyzersDiagnosticAnalyzerRunner(StartupAnalyzer);
        }

        internal override bool HasConfigure => true;

        internal override AnalyzersDiagnosticAnalyzerRunner Runner { get; }

        internal override TestSource GetSource(string scenario)
        {
            return Read(scenario);
        }
    }
}
