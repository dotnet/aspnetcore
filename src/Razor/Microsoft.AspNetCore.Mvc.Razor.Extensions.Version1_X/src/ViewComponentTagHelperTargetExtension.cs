// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X
{
    internal class ViewComponentTagHelperTargetExtension : IViewComponentTagHelperTargetExtension
    {
        private static readonly string[] PublicModifiers = new[] { "public" };

        public string TagHelperTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";

        public string ViewComponentHelperTypeName { get; set; } = "global::Microsoft.AspNetCore.Mvc.IViewComponentHelper";

        public string ViewComponentHelperVariableName { get; set; } = "_helper";

        public string ViewComponentInvokeMethodName { get; set; } = "InvokeAsync";

        public string HtmlAttributeNotBoundAttributeTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute";

        public string ViewContextAttributeTypeName { get; set; } = "global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute";

        public string ViewContextTypeName { get; set; } = "global::Microsoft.AspNetCore.Mvc.Rendering.ViewContext";

        public string ViewContextPropertyName { get; set; } = "ViewContext";

        public string HtmlTargetElementAttributeTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute";

        public string TagHelperProcessMethodName { get; set; } = "ProcessAsync";

        public string TagHelperContextTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext";

        public string TagHelperContextVariableName { get; set; } = "context";

        public string TagHelperOutputTypeName { get; set; } = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput";

        public string TagHelperOutputVariableName { get; set; } = "output";

        public string TagHelperOutputTagNamePropertyName { get; set; } = "TagName";

        public string TagHelperOutputContentPropertyName { get; set; } = "Content";

        public string TagHelperContentSetMethodName { get; set; } = "SetHtmlContent";

        public string TagHelperContentVariableName { get; set; } = "content";

        public string IViewContextAwareTypeName { get; set; } = "global::Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware";

        public string IViewContextAwareContextualizeMethodName { get; set; } = "Contextualize";

        public void WriteViewComponentTagHelper(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node)
        {
            // Add target element.
            WriteTargetElementString(context.CodeWriter, node.TagHelper);

            // Initialize declaration.
            using (context.CodeWriter.BuildClassDeclaration(
                PublicModifiers,
                node.ClassName,
                TagHelperTypeName,
                interfaces: null,
                typeParameters: null))
            {
                // Add view component helper.
                context.CodeWriter.WriteVariableDeclaration(
                    $"private readonly {ViewComponentHelperTypeName}",
                    ViewComponentHelperVariableName,
                    value: null);

                // Add constructor.
                WriteConstructorString(context.CodeWriter, node.ClassName);

                // Add attributes.
                WriteAttributeDeclarations(context.CodeWriter, node.TagHelper);

                // Add process method.
                WriteProcessMethodString(context.CodeWriter, node.TagHelper);
            }
        }

        private void WriteConstructorString(CodeWriter writer, string className)
        {
            writer.Write("public ")
                .Write(className)
                .Write("(")
                .Write($"{ViewComponentHelperTypeName} helper")
                .WriteLine(")");
            using (writer.BuildScope())
            {
                writer.WriteStartAssignment(ViewComponentHelperVariableName)
                    .Write("helper")
                    .WriteLine(";");
            }
        }

        private void WriteAttributeDeclarations(CodeWriter writer, TagHelperDescriptor tagHelper)
        {
            writer.Write("[")
              .Write(HtmlAttributeNotBoundAttributeTypeName)
              .WriteParameterSeparator()
              .Write(ViewContextAttributeTypeName)
              .WriteLine("]");

            writer.WriteAutoPropertyDeclaration(
                PublicModifiers,
                ViewContextTypeName,
                ViewContextPropertyName);

            foreach (var attribute in tagHelper.BoundAttributes)
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

        private void WriteProcessMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
        {
            using (writer.BuildMethodDeclaration(
                    $"public override async",
                    $"global::{typeof(Task).FullName}",
                    TagHelperProcessMethodName,
                    new Dictionary<string, string>()
                    {
                        { TagHelperContextTypeName, TagHelperContextVariableName },
                        { TagHelperOutputTypeName, TagHelperOutputVariableName }
                    }))
            {
                writer.WriteInstanceMethodInvocation(
                    $"({ViewComponentHelperVariableName} as {IViewContextAwareTypeName})?",
                    IViewContextAwareContextualizeMethodName,
                    new[] { ViewContextPropertyName });

                var methodParameters = GetMethodParameters(tagHelper);
                writer.Write("var ")
                    .WriteStartAssignment(TagHelperContentVariableName)
                    .WriteInstanceMethodInvocation($"await {ViewComponentHelperVariableName}", ViewComponentInvokeMethodName, methodParameters);
                writer.WriteStartAssignment($"{TagHelperOutputVariableName}.{TagHelperOutputTagNamePropertyName}")
                    .WriteLine("null;");
                writer.WriteInstanceMethodInvocation(
                    $"{TagHelperOutputVariableName}.{TagHelperOutputContentPropertyName}",
                    TagHelperContentSetMethodName,
                    new[] { TagHelperContentVariableName });
            }
        }

        private string[] GetMethodParameters(TagHelperDescriptor tagHelper)
        {
            var propertyNames = tagHelper.BoundAttributes.Select(attribute => attribute.GetPropertyName());
            var joinedPropertyNames = string.Join(", ", propertyNames);
            var parametersString = $"new {{ { joinedPropertyNames } }}";
            var viewComponentName = tagHelper.GetViewComponentName();
            var methodParameters = new[] { $"\"{viewComponentName}\"", parametersString };
            return methodParameters;
        }

        private void WriteTargetElementString(CodeWriter writer, TagHelperDescriptor tagHelper)
        {
            Debug.Assert(tagHelper.TagMatchingRules.Count() == 1);

            var rule = tagHelper.TagMatchingRules.First();

            writer.Write("[")
                .WriteStartMethodInvocation(HtmlTargetElementAttributeTypeName)
                .WriteStringLiteral(rule.TagName)
                .WriteLine(")]");
        }
    }
}
