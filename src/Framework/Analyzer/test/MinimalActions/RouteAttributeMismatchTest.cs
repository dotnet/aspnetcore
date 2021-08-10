// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers.MinimalActions;

public class RouteAttributeMismatchTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new MinimalActionAnalyzer());

    [Theory]
    [InlineData("{id}", new[] { "id" })]
    [InlineData("{category}/product/{group}", new[] { "category", "group" })]
    [InlineData("{category:int}/product/{group:range(10, 20)}?", new[] { "category", "group" })]
    [InlineData("{person:int}/{ssn:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}", new[] { "person", "ssn" })]
    [InlineData("{area=Home}/{controller:required}/{id=0:int}", new[] { "area", "controller", "id" })]
    public void RouteTokenizer_Works_ForSimpleRouteTemplates(string template, string[] expected)
    {
        // Arrange
        var tokenizer = new MinimalActionAnalyzer.RouteTokenEnumerator(template);
        var actual = new List<string>();

        // Act
        while (tokenizer.MoveNext())
        {
            actual.Add(tokenizer.Current.ToString());
        }

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task MinimalAction_UnusedRouteValueProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(/*MM*/""/{id}"", () => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.RouteValueIsUnused, diagnostic.Descriptor);
        Assert.Equal($"The route value 'id' does not get bound and can be removed", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
    }

    [Fact]
    public async Task MinimalAction_SomeUnusedTokens_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(/*MM*/""/{id:int}/{location:alpha}"", (int id, string loc) => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.RouteValueIsUnused, diagnostic.Descriptor);
        Assert.Equal($"The route value 'location' does not get bound and can be removed", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
    }

    [Fact]
    public async Task MinimalAction_FromRouteParameterWithMatchingToken_Works()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(/*MM*/""/{id:int}"", ([FromRoute] int id, string loc) => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_FromRouteParameterUsingName_Works()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(/*MM*/""/{userId}"", ([FromRoute(Name = ""userId"")] int id) => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_FromRouteParameterWithNoMatchingToken_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(/*MM1*/""/{userId:int}"", ([FromRoute] int /*MM2*/id, string loc) => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(
            diagnostics.OrderBy(d => d.Descriptor.Id),
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.RouteValueIsUnused, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.RouteParameterCannotBeBound, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic.Location);
            });
    }
}
