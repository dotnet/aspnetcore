// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using CSTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
    Microsoft.AspNetCore.Analyzers.Mvc.MvcAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

public class AttributeRoutingUsedForNonActionMethodTest
{
    private const string Program = """
        public class Program
        {
            public static void Main() { }
        }
        """;

    [Theory]
    [InlineData("AcceptVerbs(\"get\")")]
    [InlineData("HttpDelete")]
    [InlineData("HttpGet")]
    [InlineData("HttpHead")]
    [InlineData("HttpOptions")]
    [InlineData("HttpPatch")]
    [InlineData("HttpPost")]
    [InlineData("HttpPut")]
    [InlineData("Route(\"test\")")]
    public async Task NonPublicMethod_HasDiagnostics(string attribute)
    {
        // Arrange
        var source = $$"""
            using Microsoft.AspNetCore.Mvc;

            public class TestController : ControllerBase
            {
                [{|#0:{{attribute}}|}]
                protected object Test1() => new();

                [{|#1:{{attribute}}|}]
                private object Test2() => new();

                [{|#2:{{attribute}}|}]
                internal object Test3() => new();

                [{|#3:{{attribute}}|}]
                protected internal object Test4() => new();
            }
            """;

        var test = CSTest.Create(
                source,
                CreateDiagnostic("Test1", location: 0),
                CreateDiagnostic("Test2", location: 1),
                CreateDiagnostic("Test3", location: 2),
                CreateDiagnostic("Test4", location: 3))
            .WithSource(Program);

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [InlineData("AcceptVerbs(\"get\")")]
    [InlineData("HttpDelete")]
    [InlineData("HttpGet")]
    [InlineData("HttpHead")]
    [InlineData("HttpOptions")]
    [InlineData("HttpPatch")]
    [InlineData("HttpPost")]
    [InlineData("HttpPut")]
    [InlineData("Route(\"test\")")]
    public async Task MethodMarkedAsNonAction_HasDiagnostics(string attribute)
    {
        // Arrange
        var source = $$"""
            using Microsoft.AspNetCore.Mvc;

            public class TestController : ControllerBase
            {
                [NonAction]
                [{|#0:{{attribute}}|}]
                public object Test() => new();
            }
            """;

        var test = CSTest.Create(source, CreateDiagnostic("Test")).WithSource(Program);

        // Act & Assert
        await test.RunAsync();
    }

    private static DiagnosticResult CreateDiagnostic(string methodName, int location = 0)
    {
        return new DiagnosticResult(DiagnosticDescriptors.AttributeRoutingUsedForNonActionMethod).WithArguments(methodName).WithLocation(location);
    }
}
