// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Razor.Host;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorCodeParser : CSharpCodeParser
    {
        private const string ModelKeyword = "model";
        private const string InjectKeyword = "inject";
        private readonly string _baseType;
        private SourceLocation? _endInheritsLocation;
        private bool _modelStatementFound;

        public MvcRazorCodeParser(string baseType)
        {
            _baseType = baseType;
            MapDirectives(ModelDirective, ModelKeyword);
            MapDirectives(InjectDirective, InjectKeyword);
        }

        protected override void InheritsDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(SyntaxConstants.CSharp.InheritsKeyword);
            AcceptAndMoveNext();
            _endInheritsLocation = CurrentLocation;

            InheritsDirectiveCore();
            CheckForInheritsAndModelStatements();
        }

        private void CheckForInheritsAndModelStatements()
        {
            if (_modelStatementFound && _endInheritsLocation.HasValue)
            {
                Context.OnError(_endInheritsLocation.Value,
                                Resources.FormatMvcRazorCodeParser_CannotHaveModelAndInheritsKeyword(ModelKeyword));
            }
        }

        protected virtual void ModelDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(ModelKeyword);
            AcceptAndMoveNext();

            var endModelLocation = CurrentLocation;

            BaseTypeDirective(Resources.FormatMvcRazorCodeParser_KeywordMustBeFollowedByTypeName(ModelKeyword),
                              CreateModelCodeGenerator);

            if (_modelStatementFound)
            {
                Context.OnError(endModelLocation,
                                Resources.FormatMvcRazorCodeParser_OnlyOneModelStatementIsAllowed(ModelKeyword));
            }

            _modelStatementFound = true;

            CheckForInheritsAndModelStatements();
        }

        protected virtual void InjectDirective()
        {
            // @inject MyApp.MyService MyServicePropertyName
            AssertDirective(InjectKeyword);
            AcceptAndMoveNext();

            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept whitespace
            var remainingWs = AcceptSingleWhiteSpaceCharacter();
            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            Output(SpanKind.MetaCode);

            if (remainingWs != null)
            {
                Accept(remainingWs);
            }

            // Consume any other whitespace tokens.
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            var hasTypeError = !At(CSharpSymbolType.Identifier);
            if (hasTypeError)
            {
                Context.OnError(CurrentLocation,
                                Resources.FormatMvcRazorCodeParser_KeywordMustBeFollowedByTypeName(InjectKeyword));
            }

            // Accept 'MyApp.MyService'
            NamespaceOrTypeName();

            // typeName now contains the token 'MyApp.MyService'
            var typeName = Span.GetContent().Value;

            var propertyStartLocation = CurrentLocation;
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (!hasTypeError && (EndOfFile || At(CSharpSymbolType.NewLine)))
            {
                // Add an error for the property name only if we successfully read the type name
                Context.OnError(propertyStartLocation,
                                Resources.FormatMvcRazorCodeParser_InjectDirectivePropertyNameRequired(InjectKeyword));
            }

            // Read until end of line. Span now contains 'MyApp.MyService MyServiceName'.
            AcceptUntil(CSharpSymbolType.NewLine);
            if (!Context.DesignTimeMode)
            {
                // We want the newline to be treated as code, but it causes issues at design-time.
                Optional(CSharpSymbolType.NewLine);
            }

            // Parse out 'MyServicePropertyName' from the Span.
            var propertyName = Span.GetContent()
                               .Value
                               .Substring(typeName.Length);

            Span.CodeGenerator = new InjectParameterGenerator(typeName.Trim(),
                                                              propertyName.Trim());

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code);
        }

        private SpanCodeGenerator CreateModelCodeGenerator(string model)
        {
            return new ModelCodeGenerator(_baseType, model);
        }
    }
}
