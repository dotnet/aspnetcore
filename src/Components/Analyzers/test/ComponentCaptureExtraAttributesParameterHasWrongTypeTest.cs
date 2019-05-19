// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentCaptureExtraAttributesParameterHasWrongTypeTest : DiagnosticVerifier
    {
        [Theory]
        [InlineData("System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>")]
        [InlineData("System.Collections.Generic.Dictionary<string, object>")]
        [InlineData("System.Collections.Generic.IDictionary<string, object>")]
        [InlineData("System.Collections.Generic.IReadOnlyDictionary<string, object>")]
        public void IgnoresPropertiesWithSupportedType(string propertyType)
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureExtraAttributes = true)] {propertyType} MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IgnoresPropertiesWithCaptureExtraAttributesFalse()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureExtraAttributes = false)] string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void AddsDiagnosticForInvalidType()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureExtraAttributes = true)] string MyProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test,
                    new DiagnosticResult
                    {
                        Id = DiagnosticDescriptors.ComponentCaptureExtraAttributesParameterHasWrongType.Id,
                        Message = "Component parameter 'ConsoleApplication1.TypeName.MyProperty' defines CaptureExtraAttributes but has an unsupported type 'string'. Use a type assignable from 'System.Collections.Generic.Dictionary<string, object>'.",
                        Severity = DiagnosticSeverity.Warning,
                        Locations = new[]
                        {
                        new DiagnosticResultLocation("Test0.cs", 7, 63)
                        }
                    });
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ComponentParameterAnalyzer();
        }
    }
}
