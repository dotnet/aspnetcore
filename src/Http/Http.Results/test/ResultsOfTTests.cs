// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Http.Result;

public partial class ResultsOfTTests
{
    private readonly ITestOutputHelper _output;

    public ResultsOfTTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GeneratedCodeIsUpToDate()
    {
        // This assumes the output is in the repo artifacts directory
        var resultsOfTGeneratedPath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "ResultsOfT.Generated.cs");
        var testsGeneratedPath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "ResultsOfTTests.Generated.cs");

        var testResultsOfTGeneratedPath = Path.GetTempFileName();
        var testTestsGeneratedPath = Path.GetTempFileName();

        try
        {
            var currentResultsOfTGenerated = File.ReadAllText(resultsOfTGeneratedPath);
            var currentTestsGenerated = File.ReadAllText(testsGeneratedPath);

            ResultsOfTGenerator.Program.Run(testResultsOfTGeneratedPath, testTestsGeneratedPath);

            var testResultsOfTGenerated = File.ReadAllText(testResultsOfTGeneratedPath);
            var testTestsGenerated = File.ReadAllText(testTestsGeneratedPath);

            AssertFileContentEqual(currentResultsOfTGenerated, testResultsOfTGenerated, "ResultsOfT.Generated.cs");
            AssertFileContentEqual(currentTestsGenerated, testTestsGenerated, "ResultsOfTTests.Generated.cs");
        }
        finally
        {
            File.Delete(testResultsOfTGeneratedPath);
            File.Delete(testTestsGeneratedPath);
        }
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return httpContext;
    }

    private void AssertFileContentEqual(string expected, string actual, string type)
    {
        try
        {
            Assert.Equal(expected.Trim(), actual.Trim(), ignoreLineEndingDifferences: true);
        }
        catch (Exception)
        {
            _output.WriteLine($"Error when comparing {type}.");
            _output.WriteLine("Expected:");
            _output.WriteLine(expected);
            _output.WriteLine("Actual:");
            _output.WriteLine(actual);
            throw;
        }
    }

    private static void PopulateMetadata<TTarget>(MethodInfo method, EndpointBuilder builder) where TTarget : IEndpointMetadataProvider
    {
        TTarget.PopulateMetadata(method, builder);
    }

    private class ResultTypeProvidedMetadata
    {
        public string SourceTypeName { get; init; }
    }

    private class EmptyServiceProvider : IServiceProvider
    {
        public static IServiceProvider Instance { get; } = new EmptyServiceProvider();

        public object GetService(Type serviceType) => null;
    }
}
