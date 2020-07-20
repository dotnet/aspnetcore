// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentClosureOverLoopVariableAnalyzerTest : DiagnosticVerifier
    {
        public ComponentClosureOverLoopVariableAnalyzerTest()
        {
            Analyzer = new ComponentClosureOverLoopVariablesDiagnosticAnalzyer();
            Runner = new ComponentAnalyzerDiagnosticAnalyzerRunner(Analyzer);
        }

        private ComponentClosureOverLoopVariablesDiagnosticAnalzyer Analyzer { get; }
        private ComponentAnalyzerDiagnosticAnalyzerRunner Runner { get; }

        [Fact]
        public void FindsUseOfClosureOverLoopVariable()
        {
            var test = @"namespace Test1
{
    class Test1
    {
        private void Test()
        {
            for (int i = 0; i < 10; i++)
            {
				Action DoSomething = () => { var x = i; Console.WriteLine($""Loopvars:{x},{i}""); };
            }
        }
    }
}
";

            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 42)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 79)
                    }
                }
            );
        }

        [Fact]
        public void FindsNoUseOfClosureOverLoopVariable()
        {
            var test = @"namespace Test1
{
    class Test1
    {
        private void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
				Action DoSomething = () => { var x = lcl_I; Console.WriteLine($""Loopvars:{x},{lcl_I}""); };
            }
        }
    }
}
";

            VerifyCSharpDiagnostic(test);
        }
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new ComponentClosureOverLoopVariablesDiagnosticAnalzyer();
    }
}
