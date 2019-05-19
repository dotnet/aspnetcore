// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentCaptureExtraAttributesParameterMustBeUniqueTest : DiagnosticVerifier
    {
        [Fact]
        public void IgnoresPropertiesWithCaptureExtraAttributesFalse()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using System.Collections.Generic;
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureExtraAttributes = false)] string MyProperty {{ get; set; }}
            [Parameter(CaptureExtraAttributes = true)] Dictionary<string, object> MyOtherProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void AddsDiagnosticForMultipleCaptureExtraProperties()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using System.Collections.Generic;
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            [Parameter(CaptureExtraAttributes = true)] Dictionary<string, object> MyProperty {{ get; set; }}
            [Parameter(CaptureExtraAttributes = true)] Dictionary<string, object> MyOtherProperty {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;

            var message = @"Component type 'ConsoleApplication1.TypeName' defines properties multiple parameters with CaptureExtraAttribute. Properties: 
ConsoleApplication1.TypeName.MyOtherProperty
ConsoleApplication1.TypeName.MyProperty";

            VerifyCSharpDiagnostic(test,
                    new DiagnosticResult
                    {
                        Id = DiagnosticDescriptors.ComponentCaptureExtraAttributesParameterMustBeUnique.Id,
                        Message = message,
                        Severity = DiagnosticSeverity.Warning,
                        Locations = new[]
                        {
                        new DiagnosticResultLocation("Test0.cs", 6, 15)
                        }
                    });
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ComponentParameterAnalyzer();
        }
    }
}
