// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
public partial class UseTopLevelRouteRegistrationsInsteadOfUseEndpointsTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task DoesNotWarnWhenEndpointRegistrationIsTopLevel()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app.MapGet(""/"", () => ""Hello World!"");
";
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        //assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotWarnWhenEndpointRegistrationIsTopLevel_InMain()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.UseRouting();
        app.MapGet(""/"", () => ""Hello World!"");
    }
}
";
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        //assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotWarnWhenEndpointRegistrationIsNotTopLevel_NoInvocation()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app.UseEndpoints(e => { });
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotWarnWhenEndpointRegistrationIsNotTopLevel_NoInvocation_InMain()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(e => { });
    }
}
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WarnsWhenEndpointRegistrationIsNotTopLevel()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app./*MM*/UseEndpoints(endpoints =>
{
    endpoints.MapGet(""/"", () => ""Hello World!"");
});
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsWhenEndpointRegistrationIsNotTopLevel_OtherMapMethods()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app./*MM1*/UseEndpoints(endpoints =>
{
    endpoints.MapGet(""/"", () => ""This is a GET"");
});
app./*MM2*/UseEndpoints(endpoints =>
{
    endpoints.MapPost(""/"", () => ""This is a POST"");
});
app./*MM3*/UseEndpoints(endpoints =>
{
    endpoints.MapPut(""/"", () => ""This is a PUT"");
});
app./*MM4*/UseEndpoints(endpoints =>
{
    endpoints.MapDelete(""/"", () => ""This is a DELETE"");
});
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        Assert.Equal(4, diagnostics.Length);
        var diagnostic1 = diagnostics[0];
        var diagnostic2 = diagnostics[1];
        var diagnostic3 = diagnostics[2];
        var diagnostic4 = diagnostics[3];

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic1.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic1.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic1.GetMessage(CultureInfo.InvariantCulture));

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic2.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic2.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic2.GetMessage(CultureInfo.InvariantCulture));

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic3.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM3"], diagnostic3.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic3.GetMessage(CultureInfo.InvariantCulture));

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic2.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM4"], diagnostic4.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic2.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsWhenEndpointRegistrationIsNotTopLevel_InMain_MapControllers()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        var app = builder.Build();
        app.UseRouting();
        app./*MM*/UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsWhenEndpointRegistrationIsNotTopLevel_OnDifferentLine_WithRouteParameters()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app.
    /*MM*/UseEndpoints(endpoints =>
{
    endpoints.MapGet(""/users/{userId}/books/{bookId}"", 
        (int userId, int bookId) => $""The user id is {userId} and book id is {bookId}"");
});
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        //assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsTwiceWhenEndpointRegistrationIsNotTopLevel_OnDifferentLine()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRouting();
app./*MM1*/UseEndpoints(endpoints =>
{
    endpoints.MapGet(""/"", () => ""Hello World!"");
});
app./*MM2*/UseEndpoints(endpoints =>
{
    endpoints.MapGet(""/"", () => ""Hello World!"");
});
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        //assert
        Assert.Equal(2, diagnostics.Length);
        var diagnostic1 = diagnostics[0];
        var diagnostic2 = diagnostics[1];

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic1.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic1.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic1.GetMessage(CultureInfo.InvariantCulture));

        Assert.Same(DiagnosticDescriptors.UseTopLevelRouteRegistrationsInsteadOfUseEndpoints, diagnostic2.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic2.Location);
        Assert.Equal("Suggest using top level route registrations instead of UseEndpoints", diagnostic2.GetMessage(CultureInfo.InvariantCulture));
    }

}
