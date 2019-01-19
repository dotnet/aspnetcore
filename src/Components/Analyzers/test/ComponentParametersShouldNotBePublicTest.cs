// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentParametersShouldNotBePublic : CodeFixVerifier
    {
        static string BlazorParameterSource = $@"
    namespace {typeof(ParameterAttribute).Namespace}
    {{
        public class {typeof(ParameterAttribute).Name} : System.Attribute
        {{
        }}
    }}
";

        [Fact]
        public void IgnoresPublicPropertiesWithoutParameterAttribute()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string MyProperty { get; set; }
        }
    }" + BlazorParameterSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IgnoresNonpublicPropertiesWithParameterAttribute()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] string MyPropertyNoModifer { get; set; }
            [Parameter] private string MyPropertyPrivate { get; set; }
            [Parameter] protected string MyPropertyProtected { get; set; }
            [Parameter] internal string MyPropertyInternal { get; set; }
        }
    }" + BlazorParameterSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void AddsDiagnosticAndFixForPublicPropertiesWithParameterAttribute()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] public string BadProperty1 { get; set; }
            [Parameter] public object BadProperty2 { get; set; }
        }
    }" + BlazorParameterSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = "BL9993",
                    Message = "Component parameter 'BadProperty1' has a public setter, but component parameters should not be publicly settable.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 13)
                    }
                },
                new DiagnosticResult
                {
                    Id = "BL9993",
                    Message = "Component parameter 'BadProperty2' has a public setter, but component parameters should not be publicly settable.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 13)
                    }
                });

            VerifyCSharpFix(test, @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] string BadProperty1 { get; set; }
            [Parameter] object BadProperty2 { get; set; }
        }
    }" + BlazorParameterSource);
        }

        [Fact]
        public void IgnoresPublicPropertiesWithNonPublicSetterWithParameterAttribute()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] public string MyProperty1 { get; private set; }
            [Parameter] public object MyProperty2 { get; protected set; }
            [Parameter] public object MyProperty2 { get; internal set; }
        }
    }" + BlazorParameterSource;

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ComponentParametersShouldNotBePublicCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ComponentParametersShouldNotBePublicAnalyzer();
        }
    }
}
