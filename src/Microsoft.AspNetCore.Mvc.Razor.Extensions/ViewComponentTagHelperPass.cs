// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTagHelperPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            if (visitor.Class == null || visitor.TagHelpers.Count == 0)
            {
                // Nothing to do, bail.
                return;
            }

            foreach (var tagHelper in visitor.TagHelpers)
            {
                GenerateVCTHClass(visitor.Class, tagHelper.Value);

                var tagHelperTypeName = tagHelper.Value.Metadata[ITagHelperDescriptorBuilder.TypeNameKey];
                if (visitor.Fields.UsedTagHelperTypeNames.Remove(tagHelperTypeName))
                {
                    visitor.Fields.UsedTagHelperTypeNames.Add(GetVCTHFullName(visitor.Namespace, visitor.Class, tagHelper.Value));
                }
            }

            foreach (var createNode in visitor.CreateTagHelpers)
            {
                RewriteCreateNode(visitor.Namespace, visitor.Class, createNode);
            }
        }

        private void GenerateVCTHClass(ClassDeclarationIRNode @class, TagHelperDescriptor tagHelper)
        {
            var writer = new CSharpCodeWriter();
            WriteClass(writer, tagHelper);

            var statement = new CSharpStatementIRNode()
            {
                Parent = @class
            };
            RazorIRBuilder.Create(statement)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = writer.Builder.ToString()
                });

            @class.Children.Add(statement);
        }

        private void RewriteCreateNode(
            NamespaceDeclarationIRNode @namespace,
            ClassDeclarationIRNode @class,
            CreateTagHelperIRNode node)
        {
            var originalTypeName = node.TagHelperTypeName;

            var newTypeName = GetVCTHFullName(@namespace, @class, node.Descriptor);
            for (var i = 0; i < node.Parent.Children.Count; i++)
            {
                var setProperty = node.Parent.Children[i] as SetTagHelperPropertyIRNode;
                if (setProperty != null)
                {
                    setProperty.TagHelperTypeName = newTypeName;
                }
            }

            node.TagHelperTypeName = newTypeName;
        }

        private static string GetVCTHFullName(
            NamespaceDeclarationIRNode @namespace,
            ClassDeclarationIRNode @class,
            TagHelperDescriptor tagHelper)
        {
            var vcName = tagHelper.Metadata[ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
            return $"{@namespace.Content}.{@class.Name}.__Generated__{vcName}ViewComponentTagHelper";
        }

        private static string GetVCTHClassName(
            TagHelperDescriptor tagHelper)
        {
            var vcName = tagHelper.Metadata[ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
            return $"__Generated__{vcName}ViewComponentTagHelper";
        }

        private void WriteClass(CSharpCodeWriter writer, TagHelperDescriptor descriptor)
        {
            // Add target element.
            BuildTargetElementString(writer, descriptor);

            // Initialize declaration.
            var tagHelperTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";
            var className = GetVCTHClassName(descriptor);

            using (writer.BuildClassDeclaration("public", className, new[] { tagHelperTypeName }))
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

        private void BuildConstructorString(CSharpCodeWriter writer, string className)
        {
            var helperPair = new KeyValuePair<string, string>(
                $"global::Microsoft.AspNetCore.Mvc.IViewComponentHelper",
                "helper");

            using (writer.BuildConstructor("public", className, new[] { helperPair }))
            {
                writer.WriteStartAssignment("_helper")
                    .Write("helper")
                    .WriteLine(";");
            }
        }

        private void BuildAttributeDeclarations(CSharpCodeWriter writer, TagHelperDescriptor descriptor)
        {
            writer.Write("[")
              .Write("Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute")
              .WriteParameterSeparator()
              .Write($"global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute")
              .WriteLine("]");

            writer.WriteAutoPropertyDeclaration(
                "public",
                $"global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext",
                "ViewContext");

            foreach (var attribute in descriptor.BoundAttributes)
            {
                writer.WriteAutoPropertyDeclaration(
                    "public", attribute.TypeName, attribute.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey]);

                if (attribute.IndexerTypeName != null)
                {
                    writer.Write(" = ")
                        .WriteStartNewObject(attribute.TypeName)
                        .WriteEndMethodInvocation();
                }
            }
        }

        private void BuildProcessMethodString(CSharpCodeWriter writer, TagHelperDescriptor descriptor)
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
            var propertyNames = descriptor.BoundAttributes.Select(
                attribute => attribute.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey]);
            var joinedPropertyNames = string.Join(", ", propertyNames);
            var parametersString = $"new {{ { joinedPropertyNames } }}";

            var viewComponentName = descriptor.Metadata[
                ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
            var methodParameters = new[] { $"\"{viewComponentName}\"", parametersString };
            return methodParameters;
        }

        private void BuildTargetElementString(CSharpCodeWriter writer, TagHelperDescriptor descriptor)
        {
            Debug.Assert(descriptor.TagMatchingRules.Count() == 1);

            var rule = descriptor.TagMatchingRules.First();

            writer.Write("[")
                .WriteStartMethodInvocation("Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute")
                .WriteStringLiteral(rule.TagName)
                .WriteLine(")]");
        }

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Class { get; private set; }

            public DeclareTagHelperFieldsIRNode Fields { get; private set; }

            public NamespaceDeclarationIRNode Namespace { get; private set; }

            public List<CreateTagHelperIRNode> CreateTagHelpers { get; } = new List<CreateTagHelperIRNode>();

            public Dictionary<string, TagHelperDescriptor> TagHelpers { get; } = new Dictionary<string, TagHelperDescriptor>();

            public override void VisitCreateTagHelper(CreateTagHelperIRNode node)
            {
                var tagHelper = node.Descriptor;
                if (ViewComponentTagHelperDescriptorConventions.IsViewComponentDescriptor(tagHelper))
                {
                    // Capture all the VCTagHelpers (unique by type name) so we can generate a class for each one.
                    var vcName = tagHelper.Metadata[ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
                    TagHelpers[vcName] = tagHelper;

                    CreateTagHelpers.Add(node);
                }
            }

            public override void VisitNamespace(NamespaceDeclarationIRNode node)
            {
                if (Namespace == null)
                {
                    Namespace = node;
                }

                base.VisitNamespace(node);
            }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClass(node);
            }

            public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
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
