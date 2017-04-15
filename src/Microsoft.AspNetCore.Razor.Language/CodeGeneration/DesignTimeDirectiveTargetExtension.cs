// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DesignTimeDirectiveTargetExtension : IDesignTimeDirectiveTargetExtension
    {
        private const string DirectiveTokenHelperMethodName = "__RazorDirectiveTokenHelpers__";
        private const string TypeHelper = "__typeHelper";

        public void WriteDesignTimeDirective(CSharpRenderingContext context, DesignTimeDirectiveIRNode node)
        {
            context.Writer
                .WritePragma("warning disable 219")
                .WriteLine($"private void {DirectiveTokenHelperMethodName}() {{");

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is DirectiveTokenIRNode n)
                {
                    WriteDesignTimeDirectiveToken(context, n);
                }
            }

            context.Writer
                .WriteLine("}")
                .WritePragma("warning restore 219");
        }

        private void WriteDesignTimeDirectiveToken(CSharpRenderingContext context, DirectiveTokenIRNode node)
        {
            var tokenKind = node.Descriptor.Kind;
            if (!node.Source.HasValue ||
                !string.Equals(
                    context.SourceDocument?.FileName,
                    node.Source.Value.FilePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to handle directives from imports.
                return;
            }

            // Wrap the directive token in a lambda to isolate variable names.
            context.Writer
                .Write("((")
                .Write(typeof(Action).FullName)
                .Write(")(");
            using (context.Writer.BuildLambda(endLine: false))
            {
                var originalIndent = context.Writer.CurrentIndent;
                context.Writer.ResetIndent();
                switch (tokenKind)
                {
                    case DirectiveTokenKind.Type:

                        // {node.Content} __typeHelper = null;

                        context.AddLineMappingFor(node);
                        context.Writer
                            .Write(node.Content)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper)
                            .WriteLine("null;");
                        break;

                    case DirectiveTokenKind.Member:

                        // global::System.Object {node.content} = null;

                        context.Writer
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ");

                        context.AddLineMappingFor(node);
                        context.Writer
                            .Write(node.Content)
                            .WriteLine(" = null;");
                        break;

                    case DirectiveTokenKind.Namespace:

                        // global::System.Object __typeHelper = nameof({node.Content});

                        context.Writer
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                        context.Writer.Write("nameof(");

                        context.AddLineMappingFor(node);
                        context.Writer
                            .Write(node.Content)
                            .WriteLine(");");
                        break;

                    case DirectiveTokenKind.String:

                        // global::System.Object __typeHelper = "{node.Content}";

                        context.Writer
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                        if (node.Content.StartsWith("\"", StringComparison.Ordinal))
                        {
                            context.AddLineMappingFor(node);
                            context.Writer.Write(node.Content);
                        }
                        else
                        {
                            context.Writer.Write("\"");
                            context.AddLineMappingFor(node);
                            context.Writer
                                .Write(node.Content)
                                .Write("\"");
                        }

                        context.Writer.WriteLine(";");
                        break;
                }
                context.Writer.SetIndent(originalIndent);
            }
            context.Writer.WriteLine("))();");
        }
    }
}
