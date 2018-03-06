// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ControllerAnalyzerContext
    {
#pragma warning disable RS1012 // Start action has no registered actions.
        public ControllerAnalyzerContext(CompilationStartAnalysisContext context)
#pragma warning restore RS1012 // Start action has no registered actions.
        {
            Context = context;
            ControllerAttribute = Context.Compilation.GetTypeByMetadataName(TypeNames.ControllerAttribute);
        }

        public CompilationStartAnalysisContext Context { get; }

        public INamedTypeSymbol ControllerAttribute { get;  }

        private INamedTypeSymbol _systemThreadingTask;
        public INamedTypeSymbol SystemThreadingTask => GetType(TypeNames.Task, ref _systemThreadingTask);

        private INamedTypeSymbol _systemThreadingTaskOfT;
        public INamedTypeSymbol SystemThreadingTaskOfT => GetType(TypeNames.TaskOfT, ref _systemThreadingTaskOfT);

        public INamedTypeSymbol _nonActionAttribute;
        public INamedTypeSymbol NonActionAttribute => GetType(TypeNames.NonActionAttribute, ref _nonActionAttribute);

        private INamedTypeSymbol GetType(string name, ref INamedTypeSymbol cache) =>
            cache = cache ?? Context.Compilation.GetTypeByMetadataName(name);

        public bool IsControllerAction(IMethodSymbol method)
        {
            return
                method.ContainingType.HasAttribute(ControllerAttribute, inherit: true) &&
                method.DeclaredAccessibility == Accessibility.Public &&
                method.MethodKind == MethodKind.Ordinary &&
                !method.IsGenericMethod &&
                !method.IsAbstract &&
                !method.IsStatic &&
                !method.HasAttribute(NonActionAttribute);
        }
    }
}
