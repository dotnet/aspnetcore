// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryAddAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.Http;

public class HeaderDictionaryAddAnalyzerTests
{
    [Fact]
    public async Task IHeaderDictionary_WithAdd_ReportsDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddAnalyzerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        {|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
    }
}",
        new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
            .WithLocation(0)
            .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message));
    }

    [Fact]
    public async Task IHeaderDictionary_WithAppend_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddAnalyzerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(""Accept"", ""text/html"");
    }
}");
    }

    [Fact]
    public async Task IHeaderDictionary_WithIndexer_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddAnalyzerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[""Accept""] = ""text/html"";
    }
}");
    }
}
