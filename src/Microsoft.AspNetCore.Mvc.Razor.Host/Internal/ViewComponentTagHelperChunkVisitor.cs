// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.Host.Internal
{
    public class ViewComponentTagHelperChunkVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private readonly GeneratedViewComponentTagHelperContext _context;
        private readonly HashSet<string> _writtenViewComponents;

        private const string ViewComponentTagHelperVariable = "_viewComponentHelper";
        private const string ViewContextVariable = "ViewContext";

        public ViewComponentTagHelperChunkVisitor(CSharpCodeWriter writer, CodeGeneratorContext context) 
            : base(writer, context)
        {
            _context = new GeneratedViewComponentTagHelperContext();
            _writtenViewComponents = new HashSet<string>(StringComparer.Ordinal);
        }

        public override void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var tagHelperChunk = chunk as TagHelperChunk;
            if (tagHelperChunk != null) 
            {
                Visit(tagHelperChunk);
            }

            var parentChunk = chunk as ParentChunk;
            if (parentChunk != null)
            {
                Visit(parentChunk);
            }
        }

        protected override void Visit(ParentChunk parentChunk)
        {
            Accept(parentChunk.Children);
        }

        protected override void Visit(TagHelperChunk chunk)
        {
            foreach (var descriptor in chunk.Descriptors)
            {
                string shortName;
                if (descriptor.PropertyBag.TryGetValue(
                    ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey,
                    out shortName))
                {
                    var typeName = $"__Generated__{shortName}ViewComponentTagHelper";

                    if (_writtenViewComponents.Add(typeName))
                    {
                        WriteClass(descriptor);
                    }
                }
            }
        }

        private void WriteClass(TagHelperDescriptor descriptor)
        {
            // Add target element.
            BuildTargetElementString(descriptor);

            // Initialize declaration.
            var tagHelperTypeName = typeof(TagHelper).FullName;

            var shortName = descriptor.PropertyBag[ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
            var className = $"__Generated__{shortName}ViewComponentTagHelper";

            using (Writer.BuildClassDeclaration("public", className, new[] { tagHelperTypeName }))
            {
                // Add view component helper.
                Writer.WriteVariableDeclaration(
                    $"private readonly global::{_context.IViewComponentHelperTypeName}",
                    ViewComponentTagHelperVariable,
                    value: null);

                // Add constructor.
                BuildConstructorString(className);

                // Add attributes.
                BuildAttributeDeclarations(descriptor);

                // Add process method.
                BuildProcessMethodString(descriptor);
            }
        }

        private void BuildConstructorString(string className)
        {
            var viewComponentHelperVariable = "viewComponentHelper";

            var helperPair = new KeyValuePair<string, string>(
                $"global::{_context.IViewComponentHelperTypeName}",
                viewComponentHelperVariable);

            using (Writer.BuildConstructor( "public", className, new[] { helperPair }))
            {
                Writer.WriteStartAssignment(ViewComponentTagHelperVariable)
                    .Write(viewComponentHelperVariable)
                    .WriteLine(";");
            }
        }

        private void BuildAttributeDeclarations(TagHelperDescriptor descriptor)
        {
            Writer.Write("[")
              .Write(typeof(HtmlAttributeNotBoundAttribute).FullName)
              .WriteParameterSeparator()
              .Write($"global::{_context.ViewContextAttributeTypeName}")
              .WriteLine("]");

            Writer.WriteAutoPropertyDeclaration(
                "public",
                $"global::{_context.ViewContextTypeName}",
                ViewContextVariable);

            var indexerAttributes = descriptor.Attributes.Where(a => a.IsIndexer);

            foreach (var attribute in descriptor.Attributes)
            {
                if (attribute.IsIndexer)
                {
                    continue;
                }

                Writer.WriteAutoPropertyDeclaration("public", attribute.TypeName, attribute.PropertyName);

                if (indexerAttributes.Any(a => string.Equals(a.PropertyName, attribute.PropertyName, StringComparison.Ordinal)))
                {
                    Writer.Write(" = ")
                        .WriteStartNewObject(attribute.TypeName)
                        .WriteEndMethodInvocation();
                }
            }
        }

        private void BuildProcessMethodString(TagHelperDescriptor descriptor)
        {
            var contextVariable = "context";
            var outputVariable = "output";

            using (Writer.BuildMethodDeclaration(
                    $"public override async",
                    $"global::{typeof(Task).FullName}",
                    nameof(ITagHelper.ProcessAsync),
                    new Dictionary<string, string>()
                    {
                        { typeof(TagHelperContext).FullName, contextVariable },
                        { typeof(TagHelperOutput).FullName, outputVariable }
                    }))
            {
                Writer.WriteInstanceMethodInvocation(
                    $"({ViewComponentTagHelperVariable} as global::{_context.IViewContextAwareTypeName})?",
                    _context.ContextualizeMethodName,
                    new [] { ViewContextVariable });

                var methodParameters = GetMethodParameters(descriptor);
                var viewContentVariable = "viewContent";
                Writer.Write("var ")
                    .WriteStartAssignment(viewContentVariable)
                    .WriteInstanceMethodInvocation($"await {ViewComponentTagHelperVariable}", _context.InvokeAsyncMethodName, methodParameters);
                Writer.WriteStartAssignment($"{outputVariable}.{nameof(TagHelperOutput.TagName)}")
                    .WriteLine("null;");
                Writer.WriteInstanceMethodInvocation(
                    $"{outputVariable}.{nameof(TagHelperOutput.Content)}",
                    nameof(TagHelperContent.SetHtmlContent),
                    new [] { viewContentVariable });
            }
        }

        private string[] GetMethodParameters(TagHelperDescriptor descriptor)
        {
            var propertyNames = descriptor.Attributes.Where(a => !a.IsIndexer).Select(attribute => attribute.PropertyName);
            var joinedPropertyNames = string.Join(", ", propertyNames);
            var parametersString = $" new {{ { joinedPropertyNames } }}";

            var viewComponentName = descriptor.PropertyBag[
                ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey];
            var methodParameters = new [] { $"\"{viewComponentName}\"", parametersString };
            return methodParameters;
        }

        private void BuildTargetElementString(TagHelperDescriptor descriptor)
        {
            Writer.Write("[")
                .WriteStartMethodInvocation(typeof(HtmlTargetElementAttribute).FullName)
                .WriteStringLiteral(descriptor.FullTagName)
                .WriteLine(")]");
        }
    }
}