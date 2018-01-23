// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
{
    public static class ModelDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "model",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddTypeToken(Resources.ModelDirective_TypeToken_Name, Resources.ModelDirective_TypeToken_Description);
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                builder.Description = Resources.ModelDirective_Description;
            });

        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new Pass(builder.DesignTime));
            return builder;
        }

        public static string GetModelType(DocumentIntermediateNode document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var visitor = new Visitor();
            return GetModelType(document, visitor);
        }

        private static string GetModelType(DocumentIntermediateNode document, Visitor visitor)
        {
            visitor.Visit(document);

            for (var i = visitor.ModelDirectives.Count - 1; i >= 0; i--)
            {
                var directive = visitor.ModelDirectives[i];

                var tokens = directive.Tokens.ToArray();
                if (tokens.Length >= 1)
                {
                    return tokens[0].Content;
                }
            }

            return "dynamic";
        }

        internal class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
        {
            private readonly bool _designTime;

            public Pass(bool designTime)
            {
                _designTime = designTime;
            }

            // Runs after the @inherits directive
            public override int Order => 5;

            protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                var visitor = new Visitor();
                var modelType = GetModelType(documentNode, visitor);

                if (_designTime)
                {
                    // Alias the TModel token to a known type.
                    // This allows design time compilation to succeed for Razor files where the token isn't replaced.
                    var typeName = $"global::{typeof(object).FullName}";
                    var usingNode = new UsingDirectiveIntermediateNode()
                    {
                        Content = $"TModel = {typeName}"
                    };

                    visitor.Namespace?.Children.Insert(0, usingNode);
                }

                var baseType = visitor.Class?.BaseType?.Replace("<TModel>", "<" + modelType + ">");
                visitor.Class.BaseType = baseType;
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public NamespaceDeclarationIntermediateNode Namespace { get; private set; }

            public ClassDeclarationIntermediateNode Class { get; private set; }

            public IList<DirectiveIntermediateNode> ModelDirectives { get; } = new List<DirectiveIntermediateNode>();

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                if (Namespace == null)
                {
                    Namespace = node;
                }

                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClassDeclaration(node);
            }

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                if (node.Directive == Directive)
                {
                    ModelDirectives.Add(node);
                }
            }
        }

        #region Obsolete
        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new Pass(builder.DesignTime));
            return builder;
        }
        #endregion
    }
}
