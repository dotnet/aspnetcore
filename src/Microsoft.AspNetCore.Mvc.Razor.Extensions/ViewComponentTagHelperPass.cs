// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTagHelperPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        private static readonly string[] PublicModifiers = new[] { "public" };

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);

            if (visitor.Class == null || visitor.TagHelpers.Count == 0)
            {
                // Nothing to do, bail.
                return;
            }

            foreach (var tagHelper in visitor.TagHelpers)
            {
                GenerateVCTHClass(visitor.Class, tagHelper.Value);

                var tagHelperTypeName = tagHelper.Value.GetTypeName();
                if (visitor.Fields.UsedTagHelperTypeNames.Remove(tagHelperTypeName))
                {
                    visitor.Fields.UsedTagHelperTypeNames.Add(GetVCTHFullName(visitor.Namespace, visitor.Class, tagHelper.Value));
                }
            }

            foreach (var (parent, node) in visitor.CreateTagHelpers)
            {
                RewriteCreateNode(visitor.Namespace, visitor.Class, (CreateTagHelperIntermediateNode)node, parent);
            }
        }

        private void GenerateVCTHClass(ClassDeclarationIntermediateNode @class, TagHelperDescriptor tagHelper)
        {
            var writer = new CodeWriter();
            WriteClass(writer, tagHelper);

            var statement = new CSharpCodeIntermediateNode();
            statement.Children.Add(new IntermediateToken()
            {
                Kind = IntermediateToken.TokenKind.CSharp,
                Content = writer.GenerateCode()
            });

            @class.Children.Add(statement);
        }

        private void RewriteCreateNode(
            NamespaceDeclarationIntermediateNode @namespace,
            ClassDeclarationIntermediateNode @class,
            CreateTagHelperIntermediateNode node,
            IntermediateNode parent)
        {
            var newTypeName = GetVCTHFullName(@namespace, @class, node.Descriptor);
            for (var i = 0; i < parent.Children.Count; i++)
            {
                if (parent.Children[i] is SetTagHelperPropertyIntermediateNode setProperty &&
                    node.Descriptor.BoundAttributes.Contains(setProperty.Descriptor))
                {
                    setProperty.TagHelperTypeName = newTypeName;
                }
            }

            node.TagHelperTypeName = newTypeName;
        }

        private static string GetVCTHFullName(
            NamespaceDeclarationIntermediateNode @namespace,
            ClassDeclarationIntermediateNode @class,
            TagHelperDescriptor tagHelper)
        {
            var vcName = tagHelper.GetViewComponentName();
            return $"{@namespace.Content}.{@class.Name}.__Generated__{vcName}ViewComponentTagHelper";
        }

        private static string GetVCTHClassName(
            TagHelperDescriptor tagHelper)
        {
            var vcName = tagHelper.GetViewComponentName();
            return $"__Generated__{vcName}ViewComponentTagHelper";
        }

        private void WriteClass(CodeWriter writer, TagHelperDescriptor descriptor)
        {
            // Add target element.
            BuildTargetElementString(writer, descriptor);

            // Initialize declaration.
            var tagHelperTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";
            var className = GetVCTHClassName(descriptor);

            using (writer.BuildClassDeclaration(PublicModifiers, className, tagHelperTypeName, interfaces: null))
            {
                // Add view component helper.
                writer.WriteVariableDeclaration(
                    $"private readonly global::Microsoft.AspNetCore.Mvc.IViewComponentHelper",
                    "_helper",
                    value: null);

                // Add constructor.
                BuildConstructorString(writer, className);

                // Add attributes.
                BuildAttributeDeclarations(writer, descriptor);

                // Add process method.
                BuildProcessMethodString(writer, descriptor);
            }
        }

        private void BuildConstructorString(CodeWriter writer, string className)
        {
            writer.Write("public ")
                .Write(className)
                .Write("(")
                .Write("global::Microsoft.AspNetCore.Mvc.IViewComponentHelper helper")
                .WriteLine(")");
            using (writer.BuildScope())
            {
                writer.WriteStartAssignment("_helper")
                    .Write("helper")
                    .WriteLine(";");
            }
        }

        private void BuildAttributeDeclarations(CodeWriter writer, TagHelperDescriptor descriptor)
        {
            writer.Write("[")
              .Write("Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute")
              .WriteParameterSeparator()
              .Write($"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute")
              .WriteLine("]");

            writer.WriteAutoPropertyDeclaration(
                PublicModifiers,
                $"global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext",
                "ViewContext");

            foreach (var attribute in descriptor.BoundAttributes)
            {
                writer.WriteAutoPropertyDeclaration(
                    PublicModifiers,
                    attribute.TypeName,
                    attribute.GetPropertyName());

                if (attribute.IndexerTypeName != null)
                {
                    writer.Write(" = ")
                        .WriteStartNewObject(attribute.TypeName)
                        .WriteEndMethodInvocation();
                }
            }
        }

        private void BuildProcessMethodString(CodeWriter writer, TagHelperDescriptor descriptor)
        {
            var contextVariable = "context";
            var outputVariable = "output";

            using (writer.BuildMethodDeclaration(
                    $"public override async",
                    $"global::{typeof(Task).FullName}",
                    "ProcessAsync",
                    new Dictionary<string, string>()
                    {
                        { "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext", contextVariable },
                        { "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput", outputVariable }
                    }))
            {
                writer.WriteInstanceMethodInvocation(
                    $"(_helper as global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware)?",
                    "Contextualize",
                    new[] { "ViewContext" });

                var methodParameters = GetMethodParameters(descriptor);
                var contentVariable = "content";
                writer.Write("var ")
                    .WriteStartAssignment(contentVariable)
                    .WriteInstanceMethodInvocation($"await _helper", "InvokeAsync", methodParameters);
                writer.WriteStartAssignment($"{outputVariable}.TagName")
                    .WriteLine("null;");
                writer.WriteInstanceMethodInvocation(
                    $"{outputVariable}.Content",
                    "SetHtmlContent",
                    new[] { contentVariable });
            }
        }

        private string[] GetMethodParameters(TagHelperDescriptor descriptor)
        {
            var propertyNames = descriptor.BoundAttributes.Select(attribute => attribute.GetPropertyName());
            var joinedPropertyNames = string.Join(", ", propertyNames);
            var parametersString = $"new {{ { joinedPropertyNames } }}";

            var viewComponentName = descriptor.GetViewComponentName();
            var methodParameters = new[] { $"\"{viewComponentName}\"", parametersString };
            return methodParameters;
        }

        private void BuildTargetElementString(CodeWriter writer, TagHelperDescriptor descriptor)
        {
            Debug.Assert(descriptor.TagMatchingRules.Count() == 1);

            var rule = descriptor.TagMatchingRules.First();

            writer.Write("[")
                .WriteStartMethodInvocation("Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute")
                .WriteStringLiteral(rule.TagName)
                .WriteLine(")]");
        }

        private class Visitor : IntermediateNodeWalker
        {
            public ClassDeclarationIntermediateNode Class { get; private set; }

            public DeclareTagHelperFieldsIntermediateNode Fields { get; private set; }

            public NamespaceDeclarationIntermediateNode Namespace { get; private set; }

            public List<IntermediateNodeReference> CreateTagHelpers { get; } = new List<IntermediateNodeReference>();

            public Dictionary<string, TagHelperDescriptor> TagHelpers { get; } = new Dictionary<string, TagHelperDescriptor>();

            public override void VisitCreateTagHelper(CreateTagHelperIntermediateNode node)
            {
                var tagHelper = node.Descriptor;
                if (tagHelper.IsViewComponentKind())
                {
                    // Capture all the VCTagHelpers (unique by type name) so we can generate a class for each one.
                    var vcName = tagHelper.GetViewComponentName();
                    TagHelpers[vcName] = tagHelper;

                    CreateTagHelpers.Add(new IntermediateNodeReference(Parent, node));
                }
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                if (Namespace == null)
                {
                    Namespace = node;
                }

                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClassDeclaration(node);
            }

            public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIntermediateNode node)
            {
                if (Fields == null)
                {
                    Fields = node;
                }

                base.VisitDeclareTagHelperFields(node);
            }
        }
    }
}
