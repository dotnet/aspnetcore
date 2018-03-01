// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ComponentDocumentClassifierPass : DocumentClassifierPassBase, IRazorDocumentClassifierPass
    {
        public static readonly string ComponentDocumentKind = "Blazor.Component-0.1";

        protected override string DocumentKind => ComponentDocumentKind;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            // Treat everything as a component by default if Blazor is part of the configuration.
            return true;
        }

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument, 
            NamespaceDeclarationIntermediateNode @namespace, 
            ClassDeclarationIntermediateNode @class, 
            MethodDeclarationIntermediateNode method)
        {
            @namespace.Content = (string)codeDocument.Items[BlazorCodeDocItems.Namespace];
            if (@namespace.Content == null)
            {
                @namespace.Content = "Blazor";
            }

            @class.BaseType = BlazorApi.BlazorComponent.FullTypeName;
            @class.ClassName = (string)codeDocument.Items[BlazorCodeDocItems.ClassName];
            if (@class.ClassName == null)
            {
                @class.ClassName = codeDocument.Source.FilePath == null ? null : Path.GetFileNameWithoutExtension(codeDocument.Source.FilePath);
            }

            if (@class.ClassName == null)
            {
                @class.ClassName = "__BlazorComponent";
            }

            @class.Modifiers.Clear();
            @class.Modifiers.Add("public");

            method.ReturnType = "void";
            method.MethodName = BlazorApi.BlazorComponent.BuildRenderTree;
            method.Modifiers.Clear();
            method.Modifiers.Add("protected");
            method.Modifiers.Add("override");

            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter()
            {
                ParameterName = "builder",
                TypeName = BlazorApi.RenderTreeBuilder.FullTypeName,
            });

            // We need to call the 'base' method as the first statement.
            var callBase = new CSharpCodeIntermediateNode();
            callBase.Children.Add(new IntermediateToken
            {
                Kind = TokenKind.CSharp,
                Content = $"base.{BlazorApi.BlazorComponent.BuildRenderTree}(builder);" + Environment.NewLine
            });
            method.Children.Insert(0, callBase);
        }

        #region Workaround
        // This is a workaround for the fact that the base class doesn't provide good support
        // for replacing the IntermediateNodeWriter when building the code target. 
        void IRazorDocumentClassifierPass.Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            base.Execute(codeDocument, documentNode);
            documentNode.Target = new BlazorCodeTarget(documentNode.Options, _targetExtensions);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var feature = Engine.Features.OfType<IRazorTargetExtensionFeature>();
            _targetExtensions = feature.FirstOrDefault()?.TargetExtensions.ToArray() ?? EmptyExtensionArray;
        }

        private static readonly ICodeTargetExtension[] EmptyExtensionArray = new ICodeTargetExtension[0];
        private ICodeTargetExtension[] _targetExtensions;
        #endregion
    }
}
