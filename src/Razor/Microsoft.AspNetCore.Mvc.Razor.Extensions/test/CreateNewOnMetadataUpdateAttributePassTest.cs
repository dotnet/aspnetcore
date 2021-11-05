// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class CreateNewOnMetadataUpdateAttributePassTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Version_6_0;

    [Fact]
    public void Execute_AddsAttributes()
    {
        // Arrange
        var properties = new RazorSourceDocumentProperties(filePath: "ignored", relativePath: "Test.cshtml");
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", properties));

        var engine = CreateProjectEngine(b =>
        {
            PageDirective.Register(b);
        }).Engine; ;
        var irDocument = CreateIRDocument(engine, codeDocument);
        var pass = new CreateNewOnMetadataUpdateAttributePass
        {
            Engine = engine
        };
        var documentClassifier = new MvcViewDocumentClassifierPass { Engine = engine };

        // Act
        documentClassifier.Execute(codeDocument, irDocument);
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Collection(
            visitor.ExtensionNodes,
            node =>
            {
                var attributeNode = Assert.IsType<RazorCompiledItemMetadataAttributeIntermediateNode>(node);
                Assert.Equal("Identifier", attributeNode.Key);
                Assert.Equal("/Test.cshtml", attributeNode.Value);
            },
            node =>
            {
                Assert.IsType<CreateNewOnMetadataUpdateAttributePass.CreateNewOnMetadataUpdateAttributeIntermediateNode>(node);
            });
    }

    [Fact]
    public void Execute_NoOpsForBlazorComponents()
    {
        // Arrange
        var properties = new RazorSourceDocumentProperties(filePath: "ignored", relativePath: "Test.razor");
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", properties));
        codeDocument.SetFileKind(FileKinds.Component);

        var engine = CreateProjectEngine(b =>
        {
            PageDirective.Register(b);
        }).Engine;
        var irDocument = CreateIRDocument(engine, codeDocument);
        var pass = new CreateNewOnMetadataUpdateAttributePass
        {
            Engine = engine
        };
        var documentClassifier = new DefaultDocumentClassifierPass { Engine = engine };

        // Act
        documentClassifier.Execute(codeDocument, irDocument);
        pass.Execute(codeDocument, irDocument);
        var visitor = new Visitor();
        visitor.Visit(irDocument);

        // Assert
        Assert.Empty(visitor.ExtensionNodes);
    }

    private static DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
    {
        for (var i = 0; i < engine.Phases.Count; i++)
        {
            var phase = engine.Phases[i];
            phase.Execute(codeDocument);

            if (phase is IRazorIntermediateNodeLoweringPhase)
            {
                break;
            }
        }

        return codeDocument.GetDocumentIntermediateNode();
    }

    private class Visitor : IntermediateNodeWalker
    {
        public List<ExtensionIntermediateNode> ExtensionNodes { get; } = new();

        public override void VisitExtension(ExtensionIntermediateNode node)
        {
            ExtensionNodes.Add(node);
        }
    }
}
