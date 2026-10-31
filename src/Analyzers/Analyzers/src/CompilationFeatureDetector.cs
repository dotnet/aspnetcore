// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers;

internal static class CompilationFeatureDetector
{
    public static async Task<IImmutableSet<string>> DetectFeaturesAsync(
        Compilation compilation,
        CancellationToken cancellationToken = default)
    {
        var symbols = new StartupSymbols(compilation);
        if (!symbols.HasRequiredSymbols)
        {
            // Cannot find ASP.NET Core types.
            return ImmutableHashSet<string>.Empty;
        }

        var features = ImmutableHashSet.CreateBuilder<string>();

        // Find configure methods in the project's assembly
        var configureMethods = ConfigureMethodVisitor.FindConfigureMethods(symbols, compilation.Assembly);
        for (var i = 0; i < configureMethods.Count; i++)
        {
            var configureMethod = configureMethods[i];

            // Handles the case where a method is using partial definitions. We don't expect this to occur, but still handle it correctly.
            var syntaxReferences = configureMethod.DeclaringSyntaxReferences;
            for (var j = 0; j < syntaxReferences.Length; j++)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxReferences[j].SyntaxTree);

                var syntax = await syntaxReferences[j].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(syntax, cancellationToken);

                // Look for a call to one of the SignalR gestures that applies to the Configure method.
                if (operation
                    .Descendants()
                    .OfType<IInvocationOperation>()
                    .Any(op => StartupFacts.IsSignalRConfigureMethodGesture(op.TargetMethod)))
                {
                    features.Add(WellKnownFeatures.SignalR);
                }
            }
        }

        return features.ToImmutable();
    }
}
