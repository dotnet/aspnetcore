using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    [Generator]
    internal partial class HubProxyGenerator : ISourceGenerator
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
            var spec = parser.Parse(((SyntaxReceiver) context.SyntaxReceiver).Candidates);
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
                    Name: GenericNameSyntax
                    {
                        TypeArgumentList:
                        {
                            Arguments: { Count: 1 }
                        },
                        Identifier:
                        {
                            ValueText: "GetProxy"
                        }
                    },
                } getProxyCall)
                {
                    Candidates.Add(getProxyCall);
                }
            }
        }
    }
}
