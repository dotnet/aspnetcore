// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    [Generator]
    internal partial class CallbackRegistrationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is null)
            {
                // nothing to do yet
                return;
            }

            var parser = new Parser(context);
            var spec = parser.Parse(((SyntaxReceiver)context.SyntaxReceiver).Candidates);
            var emitter = new Emitter(context, spec);
            emitter.Emit();
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MemberAccessExpressionSyntax> Candidates { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MemberAccessExpressionSyntax
                    {
                        Name:
                        {
                            Identifier:
                            {
                                ValueText: "RegisterCallbackProvider"
                            }
                        }
                    } registrationCall)
                {
                    Candidates.Add(registrationCall);
                }
            }
        }
    }
}
