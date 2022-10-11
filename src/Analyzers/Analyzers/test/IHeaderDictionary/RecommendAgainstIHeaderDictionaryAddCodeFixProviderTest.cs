// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

public class RecommendAgainstIHeaderDictionaryAddCodeFixProviderTest
{
    [Fact]
    public async Task CodeFix_ReplacesAddWithAppend()
    {
        var source = @"
using Microsoft.AspNetCore.Http;

namespace RecommendAgainstIHeaderDictionaryAddCodeFixProviderTest;

public class Test
{
    public void Method()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Add(""Accept"", ""text/html"");
    }
}
";

        var fixedSource = @"
using Microsoft.AspNetCore.Http;

namespace RecommendAgainstIHeaderDictionaryAddCodeFixProviderTest;

public class Test
{
    public void Method()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(""Accept"", ""text/html"");
    }
}
";

        await RunTest(source, 0, fixedSource);
    }

    [Fact]
    public async Task CodeFix_ReplacesAddWithIndexer()
    {
        var source = @"
using Microsoft.AspNetCore.Http;

namespace RecommendAgainstIHeaderDictionaryAddCodeFixProviderTest;

public class Test
{
    public void Method()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Add(""Accept"", ""text/html"");
    }
}
";

        var fixedSource = @"
using Microsoft.AspNetCore.Http;

namespace RecommendAgainstIHeaderDictionaryAddCodeFixProviderTest;

public class Test
{
    public void Method()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[""Accept""] = ""text/html"";
    }
}
";

        await RunTest(source, 1, fixedSource);
    }

    private async Task RunTest(string source, int codeFixIndex, string fixedSource)
    {
        // Arrange
        var analyzerRunner = new TestAnalyzerRunner(new RecommendAgainstIHeaderDictionaryAddAnalyzer());
        var codeFixRunner = new CodeFixRunner();

        var project = TestAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { source });
        var documentId = project.DocumentIds[0];

        // Act + Assert
        var diagnostics = await analyzerRunner.GetDiagnosticsAsync(project);
        var diagnostic = Assert.Single(diagnostics);

        var actual = await codeFixRunner.ApplyCodeFixAsync(
            new RecommendAgainstIHeaderDictionaryAddCodeFixProvider(),
            project.GetDocument(documentId),
            diagnostic,
            codeFixIndex: codeFixIndex);

        Assert.Equal(fixedSource, actual, ignoreLineEndingDifferences: true);
    }
}
