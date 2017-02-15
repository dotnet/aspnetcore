// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDocumentClassifierPass : DocumentClassifierPassBase
    {
        public override int Order => RazorIRPass.DefaultFeatureOrder;

        protected override string DocumentKind => "default";

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            return true;
        }

        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument,
            NamespaceDeclarationIRNode @namespace,
            ClassDeclarationIRNode @class,
            RazorMethodDeclarationIRNode method)
        {
            var configuration = Engine.Features.OfType<DefaultDocumentClassifierPassFeature>().FirstOrDefault();
            if (configuration != null)
            {
                for (var i = 0; i < configuration.ConfigureClass.Count; i++)
                {
                    var configureClass = configuration.ConfigureClass[i];
                    configureClass(codeDocument, @class);
                }

                for (var i = 0; i < configuration.ConfigureNamespace.Count; i++)
                {
                    var configureNamespace = configuration.ConfigureNamespace[i];
                    configureNamespace(codeDocument, @namespace);
                }

                for (var i = 0; i < configuration.ConfigureMethod.Count; i++)
                {
                    var configureMethod = configuration.ConfigureMethod[i];
                    configureMethod(codeDocument, @method);
                }
            }
        }
    }
}
