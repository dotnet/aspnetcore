// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    /// <summary>
    /// A <see cref="CodeAction"/> that adds one or more <c>ProducesResponseType</c> attributes on the action.
    /// 1) It get status codes from ProducesResponseType, ProducesDefaultResponseType, and conventions applied to the action to get the declared metadata.
    /// 2) It inspects return statements to get actual metadata.
    /// Diffing the two gets us a list of undocumented status codes.
    /// We'll attempt to generate a [ProducesResponseType(typeof(SomeModel), 4xx)] if
    ///     a) the status code is 4xx or later.
    ///     b) the return statement included a return type.
    ///     c) the return type wasn't the error type (specified by ProducesErrorResponseType or implicit ProblemDetails)
    /// In all other cases, we generate [ProducesResponseType(StatusCode)]
    /// </summary>
    internal sealed class AddResponseTypeAttributeCodeFixAction : CodeAction
    {
        private readonly Document _document;
        private readonly Diagnostic _diagnostic;

        public AddResponseTypeAttributeCodeFixAction(Document document, Diagnostic diagnostic)
        {
            _document = document;
            _diagnostic = diagnostic;
        }

        public override string EquivalenceKey => _diagnostic.Location.ToString();

        public override string Title => "Add ProducesResponseType attributes.";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var nullableContext = await CreateCodeActionContext(cancellationToken).ConfigureAwait(false);
            if (nullableContext == null)
            {
                return _document;
            }

            var context = nullableContext.Value;

            var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(context.SymbolCache, context.Method);
            var errorResponseType = SymbolApiResponseMetadataProvider.GetErrorResponseType(context.SymbolCache, context.Method);

            var results = CalculateStatusCodesToApply(context, declaredResponseMetadata);
            if (results.Count == 0)
            {
                return _document;
            }

            var documentEditor = await DocumentEditor.CreateAsync(_document, cancellationToken).ConfigureAwait(false);

            var addUsingDirective = false;
            foreach (var item in results.OrderBy(s => s.statusCode))
            {
                var statusCode = item.statusCode;
                var returnType = item.typeSymbol;

                AttributeSyntax attributeSyntax;
                bool addUsing;

                if (statusCode >= 400 && returnType != null && returnType != errorResponseType)
                {
                    // If a returnType was discovered and is different from the errorResponseType, use it in the result.
                    attributeSyntax = CreateProducesResponseTypeAttribute(context, statusCode, returnType, out addUsing);
                }
                else
                {
                    attributeSyntax = CreateProducesResponseTypeAttribute(context, statusCode, out addUsing);
                }

                documentEditor.AddAttribute(context.MethodSyntax, attributeSyntax);
                addUsingDirective |= addUsing;
            }

            if (!declaredResponseMetadata.Any(m => m.IsDefault && m.AttributeSource == context.Method))
            {
                // Add a ProducesDefaultResponseTypeAttribute if the method does not already have one.
                documentEditor.AddAttribute(context.MethodSyntax, CreateProducesDefaultResponseTypeAttribute());
            }

            var apiConventionMethodAttribute = context.Method.GetAttributes(context.SymbolCache.ApiConventionMethodAttribute).FirstOrDefault();

            if (apiConventionMethodAttribute != null)
            {
                // Remove [ApiConventionMethodAttribute] declared on the method since it's no longer required
                var attributeSyntax = await apiConventionMethodAttribute
                    .ApplicationSyntaxReference
                    .GetSyntaxAsync(cancellationToken)
                    .ConfigureAwait(false);

                documentEditor.RemoveNode(attributeSyntax);
            }

            var document = documentEditor.GetChangedDocument();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root is CompilationUnitSyntax compilationUnit && addUsingDirective)
            {
                const string @namespace = "Microsoft.AspNetCore.Http";

                var declaredUsings = new HashSet<string>(compilationUnit.Usings.Select(x => x.Name.ToString()));

                if (!declaredUsings.Contains(@namespace))
                {
                    root = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(@namespace)));
                }
            }

            return document.WithSyntaxRoot(root);
        }

        private async Task<CodeActionContext?> CreateCodeActionContext(CancellationToken cancellationToken)
        {
            var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodReturnStatement = (ReturnStatementSyntax)root.FindNode(_diagnostic.Location.SourceSpan);
            var methodSyntax = methodReturnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var method = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);

            var statusCodesType = semanticModel.Compilation.GetTypeByMetadataName(ApiSymbolNames.HttpStatusCodes);
            var statusCodeConstants = GetStatusCodeConstants(statusCodesType);

            if (!ApiControllerSymbolCache.TryCreate(semanticModel.Compilation, out var symbolCache))
            {
                return null;
            }

            var codeActionContext = new CodeActionContext(semanticModel, symbolCache, method, methodSyntax, statusCodeConstants, cancellationToken);
            return codeActionContext;
        }

        private static Dictionary<int, string> GetStatusCodeConstants(INamedTypeSymbol statusCodesType)
        {
            var statusCodeConstants = new Dictionary<int, string>();

            if (statusCodesType != null)
            {
                foreach (var member in statusCodesType.GetMembers())
                {
                    if (member is IFieldSymbol field &&
                        field.Type.SpecialType == SpecialType.System_Int32 &&
                        field.Name.StartsWith("Status") &&
                        field.HasConstantValue &&
                        field.ConstantValue is int statusCode)
                    {
                        statusCodeConstants[statusCode] = field.Name;
                    }
                }
            }

            return statusCodeConstants;
        }

        private ICollection<(int statusCode, ITypeSymbol? typeSymbol)> CalculateStatusCodesToApply(in CodeActionContext context, IList<DeclaredApiResponseMetadata> declaredResponseMetadata)
        {
            if (!ActualApiResponseMetadataFactory.TryGetActualResponseMetadata(context.SymbolCache, context.SemanticModel, context.MethodSyntax, context.CancellationToken, out var actualResponseMetadata))
            {
                // If we cannot parse metadata correctly, don't offer fixes.
                return Array.Empty<(int, ITypeSymbol?)>();
            }

            var statusCodes = new Dictionary<int, (int, ITypeSymbol?)>();
            foreach (var metadata in actualResponseMetadata)
            {
                if (DeclaredApiResponseMetadata.TryGetDeclaredMetadata(declaredResponseMetadata, metadata, result: out var declaredMetadata) &&
                    declaredMetadata.AttributeSource == context.Method)
                {
                    // A ProducesResponseType attribute is declared on the method for the current status code.
                    continue;
                }

                var statusCode = metadata.IsDefaultResponse ? 200 : metadata.StatusCode;
                if (!statusCodes.ContainsKey(statusCode))
                {
                    // If a status code appears multiple times in the actual metadata, pick the first one to
                    // appear in the codefix
                    statusCodes.Add(statusCode, (statusCode, metadata.ReturnType));
                }
            }

            return statusCodes.Values;
        }

        private static AttributeSyntax CreateProducesResponseTypeAttribute(in CodeActionContext context, int statusCode, out bool addUsingDirective)
        {
            // [ProducesResponseType(StatusCodes.Status400NotFound)]
            var statusCodeSyntax = CreateStatusCodeSyntax(context, statusCode, out addUsingDirective);

            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(ApiSymbolNames.ProducesResponseTypeAttribute)
                    .WithAdditionalAnnotations(Simplifier.Annotation),
                SyntaxFactory.AttributeArgumentList().AddArguments(

                    SyntaxFactory.AttributeArgument(statusCodeSyntax)));
        }

        private static AttributeSyntax CreateProducesResponseTypeAttribute(in CodeActionContext context, int statusCode, ITypeSymbol typeSymbol, out bool addUsingDirective)
        {
            // [ProducesResponseType(typeof(ReturnType), StatusCodes.Status400NotFound)]
            var statusCodeSyntax = CreateStatusCodeSyntax(context, statusCode, out addUsingDirective);
            var responseTypeAttribute = SyntaxFactory.TypeOfExpression(
                SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .WithAdditionalAnnotations(Simplifier.Annotation));

            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(ApiSymbolNames.ProducesResponseTypeAttribute)
                    .WithAdditionalAnnotations(Simplifier.Annotation),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(responseTypeAttribute),
                    SyntaxFactory.AttributeArgument(statusCodeSyntax)));
        }

        private static ExpressionSyntax CreateStatusCodeSyntax(CodeActionContext context, int statusCode, out bool addUsingDirective)
        {
            if (context.StatusCodeConstants.TryGetValue(statusCode, out var constantName))
            {
                addUsingDirective = true;
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseTypeName(ApiSymbolNames.HttpStatusCodes)
                        .WithAdditionalAnnotations(Simplifier.Annotation),
                    SyntaxFactory.IdentifierName(constantName));
            }

            addUsingDirective = false;
            return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(statusCode));
        }

        private static AttributeSyntax CreateProducesDefaultResponseTypeAttribute()
        {
            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(ApiSymbolNames.ProducesDefaultResponseTypeAttribute)
                    .WithAdditionalAnnotations(Simplifier.Annotation));
        }

        private readonly struct CodeActionContext
        {
            public CodeActionContext(SemanticModel semanticModel,
                ApiControllerSymbolCache symbolCache,
                IMethodSymbol method,
                MethodDeclarationSyntax methodSyntax,
                Dictionary<int, string> statusCodeConstants,
                CancellationToken cancellationToken)
            {
                SemanticModel = semanticModel;
                SymbolCache = symbolCache;
                Method = method;
                MethodSyntax = methodSyntax;
                StatusCodeConstants = statusCodeConstants;
                CancellationToken = cancellationToken;
            }

            public MethodDeclarationSyntax MethodSyntax { get; }

            public Dictionary<int, string> StatusCodeConstants { get; }

            public IMethodSymbol Method { get; }

            public SemanticModel SemanticModel { get; }

            public ApiControllerSymbolCache SymbolCache { get; }

            public CancellationToken CancellationToken { get; }
        }
    }
}
