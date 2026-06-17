// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.AspNetCore.Analyzers.Dependencies;

public partial class ExtensionMethodsCompletionProviderTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    public static object[][] CompletionTriggers =>
    [
        [CompletionTrigger.Invoke],
        [null]
    ];

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task ProvidesAddOpenApiCompletion(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.$$
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.Contains(result.Completions.ItemsList, item => item.DisplayText == "AddOpenApi");
    }

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task ProvidesAddOpenApiCompletionWithPartialToken(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.[|Ad$$|]
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.Contains(result.Completions.ItemsList, item => item.DisplayText == "AddOpenApi");
    }

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task DoesNotProvideCompletionIfNoStringMatchForServices(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.[|Confi$$|]
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.DoesNotContain(result.Completions.ItemsList, item => item.DisplayText == "AddOpenApi");
    }

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task ProvidesMapOpenApiCompletion(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var app = WebApplication.Create();
        app.$$
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.Contains(result.Completions.ItemsList, item => item.DisplayText == "MapOpenApi");
    }

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task ProvidesMapOpenApiCompletionWithPartialToken(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var app = WebApplication.Create();
        app.[|Ma$$|]
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.Contains(result.Completions.ItemsList, item => item.DisplayText == "MapOpenApi");
    }

    [Theory]
    [MemberData(nameof(CompletionTriggers))]
    public async Task DoesNotProvideCompletionIfNoStringMatchForWebApplication(CompletionTrigger trigger)
    {
        // Arrange & Act
        var result = await GetCompletionsAndServiceAsync(@"
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        var app = WebApplication.Create();
        app.[|Use$$|]
    }
}
", trigger);

        // Assert
        Assert.True(result.ShouldTriggerCompletion);
        Assert.DoesNotContain(result.Completions.ItemsList, item => item.DisplayText == "MapOpenApi");
    }

    private Task<CompletionResult> GetCompletionsAndServiceAsync(string source, CompletionTrigger? completionTrigger = null)
    {
        return CompletionTestHelpers.GetCompletionsAndServiceAsync(Runner, source, completionTrigger);
    }
}
