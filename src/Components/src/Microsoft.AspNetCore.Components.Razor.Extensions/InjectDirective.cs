// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Razor
{
    // Much of the following is equivalent to Microsoft.AspNetCore.Mvc.Razor.Extensions's InjectDirective,
    // but this one outputs properties annotated for Blazor's property injector, plus it doesn't need to
    // support multiple CodeTargets.

    internal class InjectDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "inject",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddTypeToken("TypeName", "The type of the service to inject.");
                builder.AddMemberToken("PropertyName", "The name of the property.");
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = "Inject a service from the application's service container into a property.";
            });

        public static void Register(RazorProjectEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            builder.Features.Add(new Pass());
        }

        private class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
        {
            protected override void ExecuteCore(
                RazorCodeDocument codeDocument,
                DocumentIntermediateNode documentNode)
            {
                var visitor = new Visitor();
                visitor.Visit(documentNode);

                var properties = new HashSet<string>(StringComparer.Ordinal);
                var classNode = documentNode.FindPrimaryClass();

                for (var i = visitor.Directives.Count - 1; i >= 0; i--)
                {
                    var directive = visitor.Directives[i];
                    var tokens = directive.Tokens.ToArray();
                    if (tokens.Length < 2)
                    {
                        continue;
                    }

                    var typeName = tokens[0].Content;
                    var memberName = tokens[1].Content;

                    if (!properties.Add(memberName))
                    {
                        continue;
                    }

                    classNode.Children.Add(new InjectIntermediateNode(typeName, memberName));
                }
            }

            private class Visitor : IntermediateNodeWalker
            {
                public IList<DirectiveIntermediateNode> Directives { get; }
                    = new List<DirectiveIntermediateNode>();

                public override void VisitDirective(DirectiveIntermediateNode node)
                {
                    if (node.Directive == Directive)
                    {
                        Directives.Add(node);
                    }
                }
            }

            internal class InjectIntermediateNode : ExtensionIntermediateNode
            {
                private static readonly IList<string> _injectedPropertyModifiers = new[]
                {
                    $"[global::{ComponentsApi.InjectAttribute.FullTypeName}]",
                    "private" // Encapsulation is the default
                };

                public string TypeName { get; }
                public string MemberName { get; }
                public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

                public InjectIntermediateNode(string typeName, string memberName)
                {
                    TypeName = typeName;
                    MemberName = memberName;
                }

                public override void Accept(IntermediateNodeVisitor visitor)
                    => AcceptExtensionNode(this, visitor);

                public override void WriteNode(CodeTarget target, CodeRenderingContext context)
                    => context.CodeWriter.WriteAutoPropertyDeclaration(
                        _injectedPropertyModifiers,
                        TypeName,
                        MemberName);
            }
        }
    }
}
