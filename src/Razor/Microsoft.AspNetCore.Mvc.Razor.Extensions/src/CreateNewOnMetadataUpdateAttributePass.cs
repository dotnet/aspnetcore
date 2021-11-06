// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

/// <summary>
/// Applies attributes required for hot reload.
/// </summary>
internal sealed class CreateNewOnMetadataUpdateAttributePass : IntermediateNodePassBase, IRazorOptimizationPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (documentNode.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
            documentNode.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
        {
            // Not a MVC file. Skip.
            return;
        }

        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();

        var classIndex = @namespace.Children.IndexOf(@class);
        if (classIndex == -1)
        {
            return;
        }

        var identifierFeature = Engine.Features.OfType<IMetadataIdentifierFeature>().First();
        var identifier = identifierFeature.GetIdentifier(codeDocument, codeDocument.Source);

        var metadataAttributeNode = new CreateNewOnMetadataUpdateAttributeIntermediateNode();
        // Metadata attributes need to be inserted right before the class declaration.
        @namespace.Children.Insert(classIndex, metadataAttributeNode);

        // [global:Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemMetadataAttribute("Identifier", "/Views/Home/Index.cshtml")]
        @namespace.Children.Insert(classIndex, new RazorCompiledItemMetadataAttributeIntermediateNode
        {
            Key = "Identifier",
            Value = identifier,
        });
    }

    internal sealed class CreateNewOnMetadataUpdateAttributeIntermediateNode : ExtensionIntermediateNode
    {
        private const string CreateNewOnMetadataUpdateAttributeName = "global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute";

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            AcceptExtensionNode(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            // [global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute]
            context.CodeWriter
                .Write("[")
                .Write(CreateNewOnMetadataUpdateAttributeName)
                .WriteLine("]");
        }
    }
}
