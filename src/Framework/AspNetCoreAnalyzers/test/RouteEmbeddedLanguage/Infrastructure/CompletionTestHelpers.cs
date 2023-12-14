// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class CompletionTestHelpers
{
    public static async Task<CompletionResult> GetCompletionsAndServiceAsync(TestDiagnosticAnalyzerRunner runner, string source, CompletionTrigger? completionTrigger = null)
    {
        MarkupTestFile.GetPositionAndSpans(source, out var output, out int cursorPosition, out var textSpans);

        var results = completionTrigger != null
            ? await runner.GetCompletionsAndServiceAsync(cursorPosition, completionTrigger.Value, output)
            : await runner.GetCompletionsAndServiceAsync(cursorPosition, output);

        if (results.ShouldTriggerCompletion)
        {
            if (textSpans.Length > 0)
            {
                Assert.Equal(textSpans.Single(), results.CompletionListSpan);
            }
            else
            {
                Assert.Equal(cursorPosition, results.CompletionListSpan.Start);
                Assert.Equal(cursorPosition, results.CompletionListSpan.End);
            }
        }

        return results;
    }
}
