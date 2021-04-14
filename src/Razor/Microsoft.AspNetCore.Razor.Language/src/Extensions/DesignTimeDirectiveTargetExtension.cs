// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class DesignTimeDirectiveTargetExtension : IDesignTimeDirectiveTargetExtension
    {
        private const string DirectiveTokenHelperMethodName = "__RazorDirectiveTokenHelpers__";
        private const string TypeHelper = "__typeHelper";

        public void WriteDesignTimeDirective(CodeRenderingContext context, DesignTimeDirectiveIntermediateNode node)
        {
            context.CodeWriter
                .WriteLine("#pragma warning disable 219")
                .WriteLine($"private void {DirectiveTokenHelperMethodName}() {{");

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is DirectiveTokenIntermediateNode n)
                {
                    WriteDesignTimeDirectiveToken(context, n);
                }
            }

            context.CodeWriter
                .WriteLine("}")
                .WriteLine("#pragma warning restore 219");
        }

        private void WriteDesignTimeDirectiveToken(CodeRenderingContext context, DirectiveTokenIntermediateNode node)
        {
            var tokenKind = node.DirectiveToken.Kind;
            if (!node.Source.HasValue ||
                !string.Equals(
                    context.SourceDocument?.FilePath,
                    node.Source.Value.FilePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to handle directives from imports.
                return;
            }

            if (tokenKind == DirectiveTokenKind.Attribute || tokenKind == DirectiveTokenKind.GenericTypeConstraint)
            {
                // We don't need to do anything special here.
                // We let the Roslyn take care of providing syntax errors for C# attributes and generic type constraints.
                return;
            }

            // Wrap the directive token in a lambda to isolate variable names.
            context.CodeWriter
                .Write("((")
                .Write(typeof(Action).FullName)
                .Write(")(");
            using (context.CodeWriter.BuildLambda())
            {
                var originalIndent = context.CodeWriter.CurrentIndent;
                context.CodeWriter.CurrentIndent = 0;
                switch (tokenKind)
                {
                    case DirectiveTokenKind.Type:

                        if (string.IsNullOrEmpty(node.Content))
                        {
                            // This is most likely a marker token.
                            WriteMarkerToken(context, node);
                            break;
                        }

                        // {node.Content} __typeHelper = default({node.Content});
                        using (context.CodeWriter.BuildLinePragma(node.Source, context))
                        {
                            context.AddSourceMappingFor(node);
                            context.CodeWriter
                                .Write(node.Content)
                                .Write(" ")
                                .WriteStartAssignment(TypeHelper)
                                .Write("default");

                            if (!context.Options.SuppressNullabilityEnforcement)
                            {
                                context.CodeWriter.Write("!");
                            }

                            context.CodeWriter.WriteLine(";");
                        }
                        break;

                    case DirectiveTokenKind.Member:

                        if (string.IsNullOrEmpty(node.Content))
                        {
                            // This is most likely a marker token.
                            WriteMarkerToken(context, node);
                            break;
                        }

                        // global::System.Object {node.content} = null;
                        using (context.CodeWriter.BuildLinePragma(node.Source, context))
                        {
                            context.CodeWriter
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ");

                            context.AddSourceMappingFor(node);
                            context.CodeWriter
                                .Write(node.Content)
                                .Write(" = null");

                            if (!context.Options.SuppressNullabilityEnforcement)
                            {
                                context.CodeWriter.Write("!");
                            }

                            context.CodeWriter.WriteLine(";");
                        }
                        break;

                    case DirectiveTokenKind.Namespace:

                        if (string.IsNullOrEmpty(node.Content))
                        {
                            // This is most likely a marker token.
                            WriteMarkerToken(context, node);
                            break;
                        }

                        // global::System.Object __typeHelper = nameof({node.Content});
                        using (context.CodeWriter.BuildLinePragma(node.Source, context))
                        {
                            context.CodeWriter
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                            context.CodeWriter.Write("nameof(");

                            context.AddSourceMappingFor(node);
                            context.CodeWriter
                                .Write(node.Content)
                                .WriteLine(");");
                        }
                        break;

                    case DirectiveTokenKind.String:

                        // global::System.Object __typeHelper = "{node.Content}";
                        using (context.CodeWriter.BuildLinePragma(node.Source, context))
                        {
                            context.CodeWriter
                            .Write("global::")
                            .Write(typeof(object).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                            if (node.Content.StartsWith("\"", StringComparison.Ordinal))
                            {
                                context.AddSourceMappingFor(node);
                                context.CodeWriter.Write(node.Content);
                            }
                            else
                            {
                                context.CodeWriter.Write("\"");
                                context.AddSourceMappingFor(node);
                                context.CodeWriter
                                    .Write(node.Content)
                                    .Write("\"");
                            }

                            context.CodeWriter.WriteLine(";");
                        }
                        break;

                    case DirectiveTokenKind.Boolean:
                        // global::System.Boolean __typeHelper = {node.Content};
                        using (context.CodeWriter.BuildLinePragma(node.Source, context))
                        {
                            context.CodeWriter
                            .Write("global::")
                            .Write(typeof(bool).FullName)
                            .Write(" ")
                            .WriteStartAssignment(TypeHelper);

                            context.AddSourceMappingFor(node);
                            context.CodeWriter.Write(node.Content);
                            context.CodeWriter.WriteLine(";");
                        }
                        break;
                }
                context.CodeWriter.CurrentIndent = originalIndent;
            }
            context.CodeWriter.WriteLine("))();");
        }

        private void WriteMarkerToken(CodeRenderingContext context, DirectiveTokenIntermediateNode node)
        {
            // Marker tokens exist to be filled with other content a user might write. In an end-to-end
            // scenario markers prep the Razor documents C# projections to have an empty projection that
            // can be filled with other user content. This content can trigger a multitude of other events,
            // such as completion. In the case of completion, a completion session can occur when a marker
            // hasn't been filled and then we will fill it as a user types. The line pragma is necessary
            // for consistency so when a C# completion session starts, filling user code doesn't result in
            // a previously non-existent line pragma from being added and destroying the context in which
            // the completion session was started.
            using (context.CodeWriter.BuildLinePragma(node.Source, context))
            {
                context.AddSourceMappingFor(node);
                context.CodeWriter.Write(" ");
            }
        }
    }
}
