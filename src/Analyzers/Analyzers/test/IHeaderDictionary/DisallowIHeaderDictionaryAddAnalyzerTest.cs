// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

public class DisallowIHeaderDictionaryAddAnalyzerTest
{
    [Fact]
    public async Task Analyze_WithAdd_ReportsDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;

namespace DisallowIHeaderDictionaryAddAnalyzerTest;

public class Test
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        {|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
    }
}
";

        var diagnosticResult = new DiagnosticResult(DisallowIHeaderDictionaryAddAnalyzer.Diagnostics.DisallowIHeaderDictionaryAdd)
            .WithLocation(0);

        // Act + Assert
        await VerifyAnalyzerAsync(source, diagnosticResult);
    }

    [Fact]
    public async Task Analyze_WithAppend_ReportsNoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;

namespace DisallowIHeaderDictionaryAddAnalyzerTest;

public class Test
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(""Accept"", ""text/html"");
    }
}
";

        // Act + Assert
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public async Task Analyze_WithIndexer_ReportsNoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;

namespace DisallowIHeaderDictionaryAddAnalyzerTest;

public class Test
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[""Accept""] = ""text/html"";
    }
}
";

        // Act + Assert
        await VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new DisallowIHeaderDictionaryAddCSharpAnalyzerTest(new DisallowIHeaderDictionaryAddAnalyzer(), TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    internal sealed class DisallowIHeaderDictionaryAddCSharpAnalyzerTest : CSharpAnalyzerTest<DisallowIHeaderDictionaryAddAnalyzer, XUnitVerifier>
    {
        public DisallowIHeaderDictionaryAddCSharpAnalyzerTest(DisallowIHeaderDictionaryAddAnalyzer analyzer, ImmutableArray<MetadataReference> metadataReferences)
        {
            DisallowIHeaderDictionaryAddAnalyzer = analyzer;
            TestState.OutputKind = OutputKind.WindowsApplication;
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        public DisallowIHeaderDictionaryAddAnalyzer DisallowIHeaderDictionaryAddAnalyzer { get; }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { DisallowIHeaderDictionaryAddAnalyzer };
    }
}
