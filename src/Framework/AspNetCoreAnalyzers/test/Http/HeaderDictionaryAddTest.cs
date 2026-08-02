// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryAddAnalyzer,
    Microsoft.AspNetCore.Analyzers.Http.Fixers.HeaderDictionaryAddFixer>;

namespace Microsoft.AspNetCore.Analyzers.Http;

public class HeaderDictionaryAddTest
{
    private const string AppendCodeActionEquivalenceKey = "Use 'IHeaderDictionary.Append'";
    private const string IndexerCodeActionEquivalenceKey = "Use indexer";

    public static TheoryData<string, DiagnosticResult[], string> FixedWithAppendTestData => new()
    {
        // Single diagnostic
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");"
        },

        // Multiple diagnostics
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
{|#1:context.Request.Headers.Add(""Accept"", ""text/html"")|};",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message),
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(1)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");
context.Request.Headers.Append(""Accept"", ""text/html"");"
        },

        // Missing semicolon
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|}{|CS1002:|}",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html""){|CS1002:|}"
        }
    };

    [Theory]
    [MemberData(nameof(FixedWithAppendTestData))]
    public async Task IHeaderDictionary_WithAdd_FixedWithAppend(string source, DiagnosticResult[] expectedDiagnostics, string fixedSource)
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, codeActionEquivalenceKey: AppendCodeActionEquivalenceKey);
    }

    public static IEnumerable<object[]> FixedWithAppendAddsUsingDirectiveTestData()
    {
        // No existing using directives
        yield return new[]
{
            @"
var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};",

            @"
using Microsoft.AspNetCore.Http;

var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");"
        };

        // Inserted alphabetically based on existing using directives
        yield return new[]
        {
            @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};",

            @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");"
        };

        // Inserted after 'System' using directives
        yield return new[]
        {
            @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};",

            @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");"
        };
    }

    [Theory]
    [MemberData(nameof(FixedWithAppendAddsUsingDirectiveTestData))]
    public async Task IHeaderDictionary_WithAdd_FixedWithAppend_AddsUsingDirective(string source, string fixedSource)
    {
        // Source is cloned on Windows with CRLF line endings, then the test is run by Helix in Windows/Linux/macOS.
        // When Roslyn adds a new `using`, it gets added followed by Environment.NewLine.
        // For Linux/macOS, the actual result is `\n`, however, the source is cloned on Windows with CRLF expectation.
        // We replace all line endings with Environment.NewLine to avoid this.

        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(
            source.TrimStart().ReplaceLineEndings(),
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            fixedSource.TrimStart().ReplaceLineEndings(),
            codeActionEquivalenceKey: AppendCodeActionEquivalenceKey);
    }

    public static TheoryData<string, DiagnosticResult[], string> FixedWithIndexerTestData => new()
    {
        // Single diagnostic
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers[""Accept""] = ""text/html"";"
        },

        // Multiple diagnostics
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
{|#1:context.Request.Headers.Add(""Accept"", ""text/html"")|};",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message),
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(1)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers[""Accept""] = ""text/html"";
context.Request.Headers[""Accept""] = ""text/html"";"
        },

        // Missing semicolon
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|}{|CS1002:|}",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers[""Accept""] = ""text/html""{|CS1002:|}"
        },

        // With trailing trivia
        {
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
{|#0:context.Request.Headers
    .Add(""Accept"", ""text/html"")|}{|CS1002:|}",
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers[""Accept""] = ""text/html""{|CS1002:|}"
        }
    };

    [Theory]
    [MemberData(nameof(FixedWithIndexerTestData))]
    public async Task IHeaderDictionary_WithAdd_FixedWithIndexer(string source, DiagnosticResult[] expectedDiagnostics, string fixedSource)
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, codeActionEquivalenceKey: IndexerCodeActionEquivalenceKey);
    }

    [Fact]
    public async Task IHeaderDictionary_WithAppend_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers.Append(""Accept"", ""text/html"");";

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task IHeaderDictionary_WithIndexer_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;

var context = new DefaultHttpContext();
context.Request.Headers[""Accept""] = ""text/html"";";

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}
