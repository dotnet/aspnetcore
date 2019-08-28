// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    public class ComponentParameterSettersShouldBePublicTest : DiagnosticVerifier
    {
        [Fact]
        public void IgnoresCascadingParameterProperties()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(CascadingParameterAttribute).Namespace};
        class TypeName
        {{
            [CascadingParameter] string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IgnoresPublicSettersProperties()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IgnoresPrivateSettersNonParameterProperties()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            private string MyProperty {{ get; private set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ErrorsForNonPublicSetterParameters()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter] public string MyProperty1 {{ get; private set; }}
            [Parameter] public string MyProperty2 {{ get; protected set; }}
            [Parameter] public string MyProperty3 {{ get; internal set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty1' should have a public setter.",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 7, 39)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty2' should have a public setter.",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 39)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParameterSettersShouldBePublic.Id,
                    Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty3' should have a public setter.",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 39)
                    }
                });
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterAnalyzer();
    }
}
