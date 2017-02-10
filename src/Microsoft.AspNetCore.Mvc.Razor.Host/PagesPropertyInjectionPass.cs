// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public class PagesPropertyInjectionPass : IRazorIRPass
    {
        public RazorEngine Engine { get; set; }

        public int Order => RazorIRPass.LoweringOrder;

        public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            if (irDocument.DocumentKind != RazorPageDocumentClassifier.RazorPageDocumentKind)
            {
                return irDocument;
            }

            var modelType = ModelDirective.GetModelType(irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            var @class = visitor.Class;

            var viewDataType = $"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<{modelType}>";
            var vddProperty = new CSharpStatementIRNode
            {
                Content = $"public {viewDataType} ViewData => ({viewDataType})PageContext?.ViewData;",
                Parent = @class,
            };
            var modelProperty = new CSharpStatementIRNode
            {
                Content = $"public {modelType} Model => ViewData.Model;",
                Parent = @class,
            };

            @class.Children.Add(vddProperty);
            @class.Children.Add(modelProperty);

            return irDocument;
        }

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Class { get; private set; }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClass(node);
            }
        }
    }
}
