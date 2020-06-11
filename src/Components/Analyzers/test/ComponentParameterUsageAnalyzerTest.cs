// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    public class ComponentParameterUsageAnalyzerTest : DiagnosticVerifier
    {
        public ComponentParameterUsageAnalyzerTest()
        {
            ComponentTestSource = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TestComponent : IComponent
        {{
            [Parameter] public string TestProperty {{ get; set; }}
            [Parameter] public int TestInt {{ get; set; }}
            public string NonParameter {{ get; set; }}
        }}
    }}" + ComponentsTestDeclarations.Source;
        }

        private string ComponentTestSource { get; }

        [Fact]
        public void ComponentPropertySimpleAssignment_Warns()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.TestProperty = ""Hello World"";
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent.Id,
                    Message = "Component parameter 'TestProperty' should not be set outside of its component.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                });
        }

        [Fact]
        public void ComponentPropertyCoalesceAssignment__Warns()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.TestProperty ??= ""Hello World"";
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent.Id,
                    Message = "Component parameter 'TestProperty' should not be set outside of its component.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                });
        }

        [Fact]
        public void ComponentPropertyCompoundAssignment__Warns()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.TestProperty += ""Hello World"";
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent.Id,
                    Message = "Component parameter 'TestProperty' should not be set outside of its component.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                });
        }

        [Fact]
        public void ComponentPropertyIncrement_Warns()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.TestInt++;
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent.Id,
                    Message = "Component parameter 'TestInt' should not be set outside of its component.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                });
        }

        [Fact]
        public void ComponentPropertyDecrement_Warns()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.TestInt--;
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent.Id,
                    Message = "Component parameter 'TestInt' should not be set outside of its component.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 17)
                    }
                });
        }

        [Fact]
        public void ComponentPropertyExpression_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            void Method()
            {{
                System.IO.Console.WriteLine(new TestComponent().TestProperty);
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ComponentPropertyExpressionInStatement_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            void Method()
            {{
                var testComponent = new TestComponent();
                for (var i = 0; i < testComponent.TestProperty.Length; i++)
                {{
                }}
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void RetrievalOfComponentPropertyValueInAssignment_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            void Method()
            {{
                var testComponent = new TestComponent();
                AnotherProperty = testComponent.TestProperty;
            }}

            public string AnotherProperty {{ get; set; }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ShadowedComponentPropertyAssignment_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName
        {{
            void Method()
            {{
                var testComponent = new InheritedComponent();
                testComponent.TestProperty = ""Hello World"";
            }}
        }}

        class InheritedComponent : TestComponent
        {{
            public new string TestProperty {{ get; set; }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void InheritedImplicitComponentPropertyAssignment_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName : TestComponent
        {{
            void Method()
            {{
                this.TestProperty = ""Hello World"";
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ImplicitComponentPropertyAssignment_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class TypeName : IComponent
        {{
            void Method()
            {{
                TestProperty = ""Hello World"";
            }}

            [Parameter] public string TestProperty {{ get; set; }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void ComponentPropertyAssignment_NonParameter_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private TestComponent _testComponent;
            void Render()
            {{
                _testComponent = new TestComponent();
                _testComponent.NonParameter = ""Hello World"";
            }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void NonComponentPropertyAssignment_Ignores()
        {
            var test = $@"
    namespace ConsoleApplication1
    {{
        using {typeof(ParameterAttribute).Namespace};
        class OtherComponent : IComponent
        {{
            private SomethingElse _testNonComponent;
            void Render()
            {{
                _testNonComponent = new NotAComponent();
                _testNonComponent.TestProperty = ""Hello World"";
            }}
        }}
        class NotAComponent
        {{
            [Parameter] public string TestProperty {{ get; set; }}
        }}
    }}" + ComponentTestSource;

            VerifyCSharpDiagnostic(test);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterUsageAnalyzer();
    }
}
