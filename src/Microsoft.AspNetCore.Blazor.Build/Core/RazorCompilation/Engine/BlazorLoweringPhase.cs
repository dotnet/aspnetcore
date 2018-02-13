// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;

namespace Microsoft.AspNetCore.Blazor.Build.Core.RazorCompilation.Engine
{
    /// <summary>
    /// A <see cref="RazorEngine"/> phase that builds the C# document corresponding to
    /// a <see cref="RazorCodeDocument"/> for a Blazor component.
    /// </summary>
    internal class BlazorLoweringPhase : IRazorCSharpLoweringPhase
    {
        private readonly RazorCodeGenerationOptions _codegenOptions;

        public BlazorLoweringPhase(RazorCodeGenerationOptions codegenOptions)
        {
            _codegenOptions = codegenOptions
                ?? throw new ArgumentNullException(nameof(codegenOptions));
        }

        public RazorEngine Engine { get; set; }

        public void Execute(RazorCodeDocument codeDocument)
        {
            var writer = BlazorComponentDocumentWriter.Create(_codegenOptions);
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            ConvertToBlazorPrimaryMethod(documentNode);
            var csharpDoc = writer.WriteDocument(codeDocument, documentNode);
            codeDocument.SetCSharpDocument(csharpDoc);
        }

        private void ConvertToBlazorPrimaryMethod(DocumentIntermediateNode documentNode)
        {
            // Replaces the default "ExecuteAsync" method with Blazor's "BuildRenderTree".
            // Note that DefaultDocumentWriter's VisitMethodDeclaration is hardcoded to
            // emit methods with no parameters, so there's no way of setting the parameters
            // from here. We inject the parameter later in RazorCompiler.
            var primaryMethod = documentNode.FindPrimaryMethod();
            primaryMethod.ReturnType = "void";
            primaryMethod.MethodName = nameof(BlazorComponent.BuildRenderTree);
            primaryMethod.Modifiers.Clear();
            primaryMethod.Modifiers.Add("public");
            primaryMethod.Modifiers.Add("override");

            var line = new CSharpCodeIntermediateNode();
            line.Children.Add(new IntermediateToken
            {
                Kind = TokenKind.CSharp,
                Content = $"base.{primaryMethod.MethodName}(builder);" + Environment.NewLine
            });
            primaryMethod.Children.Insert(0, line);
        }

        /// <summary>
        /// Creates <see cref="DocumentWriter"/> instances that are configured to use
        /// <see cref="BlazorCodeTarget"/>.
        /// </summary>
        private class BlazorComponentDocumentWriter : DocumentWriter
        {
            public static DocumentWriter Create(RazorCodeGenerationOptions options)
                => Instance.Create(new BlazorCodeTarget(), options);

            private static BlazorComponentDocumentWriter Instance
                = new BlazorComponentDocumentWriter();

            public override RazorCSharpDocument WriteDocument(
                RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
                => throw new NotImplementedException();
        }
    }
}
