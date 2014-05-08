// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class UnclassifiedCodeSpanConstructor
    {
        private SpanConstructor _self;

        public UnclassifiedCodeSpanConstructor(SpanConstructor self)
        {
            _self = self;
        }

        public SpanConstructor AsMetaCode()
        {
            _self.Builder.Kind = SpanKind.MetaCode;
            return _self;
        }

        public SpanConstructor AsStatement()
        {
            return _self.With(new StatementCodeGenerator());
        }

        public SpanConstructor AsExpression()
        {
            return _self.With(new ExpressionCodeGenerator());
        }

        public SpanConstructor AsImplicitExpression(ISet<string> keywords)
        {
            return AsImplicitExpression(keywords, acceptTrailingDot: false);
        }

        public SpanConstructor AsImplicitExpression(ISet<string> keywords, bool acceptTrailingDot)
        {
            return _self.With(new ImplicitExpressionEditHandler(SpanConstructor.TestTokenizer, 
                                                                keywords, 
                                                                acceptTrailingDot))
                        .With(new ExpressionCodeGenerator());
        }

        public SpanConstructor AsFunctionsBody()
        {
            return _self.With(new TypeMemberCodeGenerator());
        }

        public SpanConstructor AsNamespaceImport(string ns, int namespaceKeywordLength)
        {
            return _self.With(new AddImportCodeGenerator(ns, namespaceKeywordLength));
        }

        public SpanConstructor Hidden()
        {
            return _self.With(SpanCodeGenerator.Null);
        }

        public SpanConstructor AsBaseType(string baseType)
        {
            return _self.With(new SetBaseTypeCodeGenerator(baseType));
        }

        public SpanConstructor AsRazorDirectiveAttribute(string key, string value)
        {
            return _self.With(new RazorDirectiveAttributeCodeGenerator(key, value));
        }

        public SpanConstructor As(ISpanCodeGenerator codeGenerator)
        {
            return _self.With(codeGenerator);
        }
    }
}