// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// This code is temporary. It finds source code lines of the form
    ///     @Layout<SomeType>()
    /// ... and converts them into [Layout(typeof(SomeType))] attributes on the class.
    /// Once we're able to add Blazor-specific directives and have them show up in tooling,
    /// we'll replace this with a simpler and cleaner "@Layout SomeType" directive.
    /// </summary>
    internal class TemporaryLayoutPass : TemporaryFakeDirectivePass
    {
        // Example: "Layout<MyApp.Namespace.SomeType<T1, T2>>()"
        // Captures: MyApp.Namespace.SomeType<T1, T2>
        private const string LayoutTokenPattern = @"\s*Layout\s*<(.+)\>\s*\(\s*\)\s*";

        private const string LayoutAttributeTypeName
            = "Microsoft.AspNetCore.Blazor.Layouts.LayoutAttribute";

        public static void Register(IRazorEngineBuilder configuration)
        {
            configuration.Features.Add(new TemporaryLayoutPass());
        }

        private TemporaryLayoutPass() : base(LayoutTokenPattern)
        {
        }

        protected override void HandleMatchedContent(RazorCodeDocument codeDocument, IEnumerable<string> matchedContent)
        {
            var chosenLayoutType = matchedContent.Last();
            var attributeNode = new CSharpCodeIntermediateNode();
            attributeNode.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = $"[{LayoutAttributeTypeName}(typeof ({chosenLayoutType}))]" + Environment.NewLine,
            });

            var docNode = codeDocument.GetDocumentIntermediateNode();
            var namespaceNode = docNode.FindPrimaryNamespace();
            var classNode = docNode.FindPrimaryClass();
            var classNodeIndex = namespaceNode
                .Children
                .IndexOf(classNode);
            namespaceNode.Children.Insert(classNodeIndex, attributeNode);
        }
    }
}
