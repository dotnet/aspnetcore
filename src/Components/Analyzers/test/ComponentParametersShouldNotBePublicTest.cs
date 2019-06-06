// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentParametersShouldNotBePublic : CodeFixVerifier
    {
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
    }" + ComponentsTestDeclarations.Source;

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
            [CascadingParameter] protected string MyPropertyProtected { get; set; }
            [CascadingParameter] internal string MyPropertyInternal { get; set; }
        }
    }" + ComponentsTestDeclarations.Source;

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
            [CascadingParameter] public object BadProperty2 { get; set; }
        }
    }" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBePublic.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.BadProperty1' has a public setter, but component parameters should not be publicly settable.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 39)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBePublic.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.BadProperty2' has a public setter, but component parameters should not be publicly settable.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 48)
                    }
                });

            VerifyCSharpFix(test, @"
    namespace ConsoleApplication1
    {
        using " + typeof(ParameterAttribute).Namespace + @";

        class TypeName
        {
            [Parameter] string BadProperty1 { get; set; }
            [CascadingParameter] object BadProperty2 { get; set; }
        }
    }" + ComponentsTestDeclarations.Source);
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
    }" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ComponentParametersShouldNotBePublicCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ComponentParameterAnalyzer();
        }
    }
}
