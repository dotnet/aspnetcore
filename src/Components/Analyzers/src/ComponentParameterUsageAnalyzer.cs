// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComponentParameterUsageAnalyzer : DiagnosticAnalyzer
    {
        public ComponentParameterUsageAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(new[]
            {
                DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent,
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(context =>
            {
                if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
                {
                    // Types we need are not defined.
                    return;
                }

                context.RegisterOperationBlockStartAction(startBlockContext =>
                {
                    startBlockContext.RegisterOperationAction(context =>
                    {
                        IOperation leftHandSide;

                        if (context.Operation is IAssignmentOperation assignmentOperation)
                        {
                            leftHandSide = assignmentOperation.Target;
                        }
                        else
                        {
                            var incrementOrDecrementOperation = (IIncrementOrDecrementOperation)context.Operation;
                            leftHandSide = incrementOrDecrementOperation.Target;
                        }

                        if (leftHandSide == null)
                        {
                            // Malformed assignment, no left hand side.
                            return;
                        }

                        if (leftHandSide.Kind != OperationKind.PropertyReference)
                        {
                            // We don't want to capture situations where a user does
                            // MyOtherProperty = aComponent.SomeParameter
                            return;
                        }

                        var propertyReference = (IPropertyReferenceOperation)leftHandSide;
                        var componentProperty = (IPropertySymbol)propertyReference.Member;

                        if (!ComponentFacts.IsParameter(symbols, componentProperty))
                        {
                            // This is not a property reference that we care about, it is not decorated with [Parameter].
                            return;
                        }

                        var propertyContainingType = componentProperty.ContainingType;
                        if (!ComponentFacts.IsComponent(symbols, context.Compilation, propertyContainingType))
                        {
                            // Someone referenced a property as [Parameter] inside something that is not a component.
                            return;
                        }

                        var assignmentContainingType = startBlockContext.OwningSymbol?.ContainingType;
                        if (assignmentContainingType == null)
                        {
                            // Assignment location has no containing type. Most likely we're operating on malformed code, don't try and validate.
                            return;
                        }

                        var conversion = context.Compilation.ClassifyConversion(propertyContainingType, assignmentContainingType);
                        if (conversion.Exists && conversion.IsIdentity)
                        {
                            // The assignment is taking place inside of the declaring component.
                            return;
                        }

                        if (conversion.Exists && conversion.IsExplicit)
                        {
                            // The assignment is taking place within the components type hierarchy. This means the user is setting this in a supported
                            // scenario.
                            return;
                        }

                        // At this point the user is referencing a component parameter outside of its declaring class.

                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentParametersShouldNotBeSetOutsideOfTheirDeclaredComponent,
                            propertyReference.Syntax.GetLocation(),
                            propertyReference.Member.Name));
                    }, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment, OperationKind.Increment, OperationKind.Decrement);
                });
            });
        }
    }
}
