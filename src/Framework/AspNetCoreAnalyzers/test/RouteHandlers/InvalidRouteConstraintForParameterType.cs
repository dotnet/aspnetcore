// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using CSTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
    Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public class InvalidRouteConstraintForParameterType
{
    private const string Program = $$$"""
        using Microsoft.AspNetCore.Builder;

        var webApp = WebApplication.Create();
        """;

    private static string[] IntConstraints = ["int", "min(10)", "max(10)", "range(1,10)"];
    private static string[] IntTypes = ["byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong"];

    public static TheoryData<string> MapMethods { get; } = ["Map", "MapDelete", "MapFallback", "MapGet", "MapPatch", "MapPost", "MapPut"];

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task LambdaWithValidConstraint_NoDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetValidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i++}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{{{constraint}}}}", ({{{type}}} param) => { });
                    }
                }
                """);
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task LambdaWithInvalidConstraint_HasDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetInvalidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{|#{{{i}}}:{{{constraint}}}|}}", ({{{type}}} param) => { });
                    }
                }
                """);

            test.ExpectedDiagnostics.Add(CreateDiagnostic(constraint, "param", type, location: i++));
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task LocalFunctionWithValidConstraint_NoDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetValidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i++}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{{{constraint}}}}", LocalFunction);
                
                        string LocalFunction({{{type}}} param) => param.ToString();
                    }
                }
                """);
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task LocalFunctionWithInvalidConstraint_HasDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetInvalidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{|#{{{i}}}:{{{constraint}}}|}}", LocalFunction);
                
                        string LocalFunction({{{type}}} param) => param.ToString();
                    }
                }
                """);

            test.ExpectedDiagnostics.Add(CreateDiagnostic(constraint, "param", type, location: i++));
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task InstanceMethodWithValidConstraint_NoDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetValidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i++}}}
                {
                    public static void Map(WebApplication app)
                    {
                        var handler = new Handler();
                        app.{{{methodName}}}(@"/api/{param:{{{constraint}}}}", handler.Handle);
                    }
                
                    private class Handler
                    {
                        public string Handle({{{type}}} param) => param.ToString();
                    }
                }
                """);
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task InstanceMethodWithInvalidConstraint_HasDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetInvalidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i}}}
                {
                    public static void Map(WebApplication app)
                    {
                        var handler = new Handler();
                        app.{{{methodName}}}(@"/api/{param:{|#{{{i}}}:{{{constraint}}}|}}", handler.Handle);
                    }
                
                    private class Handler
                    {
                        public string Handle({{{type}}} param) => param.ToString();
                    }
                }
                """);

            test.ExpectedDiagnostics.Add(CreateDiagnostic(constraint, "param", type, location: i++));
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task StaticMethodWithValidConstraint_NoDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetValidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i++}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{{{constraint}}}}", Handler.Handle);
                    }
                
                    private static class Handler
                    {
                        public static string Handle({{{type}}} param) => param.ToString();
                    }
                }
                """);
        }

        // Act & Assert
        await test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(MapMethods))]
    public async Task StaticMethodWithInvalidConstraint_HasDiagnostics(string methodName)
    {
        // Arrange
        var test = CSTest.Create(Program);
        var i = 0;

        foreach (var (constraint, type) in GetInvalidCombinations())
        {
            test.WithSource($$$"""
                using System;
                using Microsoft.AspNetCore.Builder;

                public static class Endpoints{{{i}}}
                {
                    public static void Map(WebApplication app)
                    {
                        app.{{{methodName}}}(@"/api/{param:{|#{{{i}}}:{{{constraint}}}|}}", Handler.Handle);
                    }
                
                    public static class Handler
                    {
                        public static string Handle({{{type}}} param) => param.ToString();
                    }
                }
                """);

            test.ExpectedDiagnostics.Add(CreateDiagnostic(constraint, "param", type, location: i++));
        }

        // Act & Assert
        await test.RunAsync();
    }

    public static IEnumerable<(string constraint, string type)> GetValidCombinations()
    {
        yield return ("bool", "bool");
        yield return ("datetime", "DateTime");
        yield return ("decimal", "decimal");
        yield return ("double", "double");
        yield return ("float", "float");
        yield return ("guid", "Guid");

        yield return ("alpha", "string");
        yield return ("file", "string");
        yield return ("nonfile", "string");

        yield return ("length(10)", "string");
        yield return ("minlength(10)", "string");
        yield return ("maxlength(10)", "string");
        yield return (@"regex(\w+)", "string");

        yield return ("long", "long");
        yield return ("long", "ulong");

        foreach (var constraint in IntConstraints)
        {
            foreach (var type in IntTypes)
            {
                yield return (constraint, type);
            }
        }
    }

    public static IEnumerable<(string constraint, string type)> GetInvalidCombinations()
    {
        yield return ("bool", "int");
        yield return ("datetime", "int");
        yield return ("decimal", "int");
        yield return ("double", "int");
        yield return ("float", "int");
        yield return ("guid", "int");

        yield return ("alpha", "int");
        yield return ("file", "int");
        yield return ("nonfile", "int");

        yield return ("length(10)", "int");
        yield return ("minlength(10)", "int");
        yield return ("maxlength(10)", "int");
        yield return (@"regex(\w+)", "int");

        yield return ("long", "byte");
        yield return ("long", "sbyte");
        yield return ("long", "short");
        yield return ("long", "ushort");
        yield return ("long", "int");
        yield return ("long", "uint");

        foreach (var constraint in IntConstraints)
        {
            yield return (constraint, "string");
        }
    }

    private static DiagnosticResult CreateDiagnostic(string constraint, string parameter, string typeName, int location = 0)
    {
        return new DiagnosticResult(DiagnosticDescriptors.InvalidRouteConstraintForParameterType)
            .WithArguments(constraint, parameter, typeName)
            .WithLocation(location);
    }
}
