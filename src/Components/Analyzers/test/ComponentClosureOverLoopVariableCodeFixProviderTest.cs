// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Test
{
    public class ComponentClosureOverLoopVariableCodeFixProviderTest : CodeFixVerifier
    {

        [Fact]
        public void AddsLocalVariableForLoopClosure_WhenNeeded()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                SomeAction( () => { var x = i; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 45)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
                SomeAction( () => { var x = lcl_I; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        [Fact]
        public void AddsLocalVariableForLoopClosure_WhenMultipleInstances()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                SomeAction( () => { var x = i; Console.WriteLine($""Loop var: {i}""); } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 45)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 11, 79)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
                SomeAction( () => { var x = lcl_I; Console.WriteLine($""Loop var: {lcl_I}""); } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        [Fact]
        public void UsesExistingLocalVariableForLoopClosure_WhenExists()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                int loopVar =   i;
                SomeAction( () => { var x = i; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 45)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                int loopVar =   i;
                SomeAction( () => { var x = loopVar; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        [Fact]
        public void AddsLocalVariableForLoopClosure_WhenExistingIsNotUseful()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                int loopVar =  2 * i;
                SomeAction( () => { var x = i; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 45)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
                int loopVar =  2 * i;
                SomeAction( () => { var x = lcl_I; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        [Fact]
        public void AddsLocalVariableForSingleLineLoopClosure_WhenNeeded()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
                SomeAction( () => { var x = i; } );
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 10, 45)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
                SomeAction( () => { var x = lcl_I; } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        [Fact]
        public void AddsLocalVariableForSingleLineLoopClosure_WhenMultipleInstances()
        {
            var test = @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
                SomeAction( () => { var x = i;  Console.WriteLine($""Loop var: {i}""); } );
        }
        void SomeAction(Action action) => action.Invoke();
    }
}";
            VerifyCSharpDiagnostic(test,
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 10, 45)
                    }
                },
                new DiagnosticResult
                {
                    Id = DiagnosticDescriptors.ClosureOverLoopVariables.Id,
                    Message = DiagnosticDescriptors.ClosureOverLoopVariables.Description.ToString(),
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 10, 80)
                    }
                }
            );

            VerifyCSharpFix(test, @"
namespace LoopTesting
{
    using System;
    class UsesClosureOverForLoopVariable
    {
        void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                var lcl_I = i;
                SomeAction( () => { var x = lcl_I;  Console.WriteLine($""Loop var: {lcl_I}""); } );
            }
        }
        void SomeAction(Action action) => action.Invoke();
    }
}");
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
            => new ComponentClosureOverLoopVariablesCodeFixProvider();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => new ComponentClosureOverLoopVariablesDiagnosticAnalzyer();
    }
}
