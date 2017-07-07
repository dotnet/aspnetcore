// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTagHelperPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after the default taghelper pass
        public override int Order => IntermediateNodePassBase.DefaultFeatureOrder + 2000;

        private static readonly string[] PublicModifiers = new[] { "public" };

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            var context = new Context(@namespace, @class);

            // For each VCTH *usage* we need to rewrite the tag helper node to use the tag helper runtime to construct
            // and set properties on the the correct field, and using the name of the type we will generate.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                foreach (var tagHelper in node.TagHelpers)
                {
                    RewriteUsage(context, node, tagHelper);
                }
            }

            // Then for each VCTH *definition* that we've seen we need to generate the class that implements
            // ITagHelper and the field that will hold it.
            foreach (var tagHelper in context.TagHelpers)
            {
                AddField(context, tagHelper);
                AddTagHelperClass(context, tagHelper);
            }
        }

        private void RewriteUsage(Context context, TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
        {
            if (!tagHelper.IsViewComponentKind())
            {
                return;
            }

            context.Add(tagHelper);

            // Now we need to insert a create node using the default tag helper runtime. This is similar to
            // code in DefaultTagHelperOptimizationPass.
            //
            // Find the body node.
            var i = 0;
            while (i < node.Children.Count && node.Children[i] is TagHelperBodyIntermediateNode)
            {
                i++;
            }
            while (i < node.Children.Count && node.Children[i] is DefaultTagHelperBodyIntermediateNode)
            {
                i++;
            }

            // Now find the last create node.
            while (i < node.Children.Count && node.Children[i] is DefaultTagHelperCreateIntermediateNode)
            {
                i++;
            }

            // Now i has the right insertion point.
            node.Children.Insert(i, new DefaultTagHelperCreateIntermediateNode()
            {
                FieldName = context.GetFieldName(tagHelper),
                TagHelper = tagHelper,
                TypeName = context.GetFullyQualifiedName(tagHelper),
            });

            // Now we need to rewrite any set property nodes to use the default runtime.
            for (i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is TagHelperPropertyIntermediateNode propertyNode &&
                    propertyNode.TagHelper == tagHelper)
                {
                    // This is a set property for this VCTH - we need to replace it with a node
                    // that will use our field and property name.
                    node.Children[i] = new DefaultTagHelperPropertyIntermediateNode(propertyNode)
                    {
                        FieldName = context.GetFieldName(tagHelper),
                        PropertyName = propertyNode.BoundAttribute.GetPropertyName(),
                    };
                }
            }
        }

        private void AddField(Context context, TagHelperDescriptor tagHelper)
        {
            // We need to insert a node for the field that will hold the tag helper. We've already generated a field name
            // at this time and use it for all uses of the same tag helper type.
            //
            // We also want to preserve the ordering of the nodes for testability. So insert at the end of any existing
            // field nodes.
            var i = 0;
            while (i < context.Class.Children.Count && context.Class.Children[i] is DefaultTagHelperRuntimeIntermediateNode)
            {
                i++;
            }

            while (i < context.Class.Children.Count && context.Class.Children[i] is FieldDeclarationIntermediateNode)
            {
                i++;
            }

            context.Class.Children.Insert(i, new FieldDeclarationIntermediateNode()
            {
                Annotations =
                {
                    { CommonAnnotations.DefaultTagHelperExtension.TagHelperField, bool.TrueString },
                },
                Modifiers =
                {
                    "private",
                },
                FieldName = context.GetFieldName(tagHelper),
                FieldType = "global::" + context.GetFullyQualifiedName(tagHelper),
            });
        }

        private void AddTagHelperClass(Context context, TagHelperDescriptor tagHelper)
        {
            var writer = new CodeWriter();
            WriteClass(context, writer, tagHelper);

            var code = new CSharpCodeIntermediateNode();
            code.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = writer.GenerateCode()
            });

            context.Class.Children.Add(code);
        }

        private void WriteClass(Context context, CodeWriter writer, TagHelperDescriptor tagHelper)
        {
            // Add target element.
            BuildTargetElementString(writer, tagHelper);

            // Initialize declaration.
            var tagHelperTypeName = "Microsoft.AspNetCore.Razor.TagHelpers.TagHelper";
            var className = context.GetClassName(tagHelper);

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
                BuildAttributeDeclarations(writer, tagHelper);

                // Add process method.
                BuildProcessMethodString(writer, tagHelper);
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

        private void BuildAttributeDeclarations(CodeWriter writer, TagHelperDescriptor tagHelper)
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

        private void BuildProcessMethodString(CodeWriter writer, TagHelperDescriptor tagHelper)
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

                var methodParameters = GetMethodParameters(tagHelper);
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

        private string[] GetMethodParameters(TagHelperDescriptor tagHelper)
        {
            var propertyNames = tagHelper.BoundAttributes.Select(attribute => attribute.GetPropertyName());
            var joinedPropertyNames = string.Join(", ", propertyNames);
            var parametersString = $"new {{ { joinedPropertyNames } }}";
            var viewComponentName = tagHelper.GetViewComponentName();
            var methodParameters = new[] { $"\"{viewComponentName}\"", parametersString };
            return methodParameters;
        }

        private void BuildTargetElementString(CodeWriter writer, TagHelperDescriptor tagHelper)
        {
            Debug.Assert(tagHelper.TagMatchingRules.Count() == 1);

            var rule = tagHelper.TagMatchingRules.First();

            writer.Write("[")
                .WriteStartMethodInvocation("Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute")
                .WriteStringLiteral(rule.TagName)
                .WriteLine(")]");
        }

        private struct Context
        {
            private Dictionary<TagHelperDescriptor, (string className, string fullyQualifiedName, string fieldName)> _tagHelpers;

            public Context(NamespaceDeclarationIntermediateNode @namespace, ClassDeclarationIntermediateNode @class)
            {
                Namespace = @namespace;
                Class = @class;

                _tagHelpers = new Dictionary<TagHelperDescriptor, (string, string, string)>();
            }

            public ClassDeclarationIntermediateNode Class { get; }

            public NamespaceDeclarationIntermediateNode Namespace { get; }


            public IEnumerable<TagHelperDescriptor> TagHelpers => _tagHelpers.Keys;

            public bool Add(TagHelperDescriptor tagHelper)
            {
                if (_tagHelpers.ContainsKey(tagHelper))
                {
                    return false;
                }

                var className = $"__Generated__{tagHelper.GetViewComponentName()}ViewComponentTagHelper";
                var fullyQualifiedName = $"{Namespace.Content}.{Class.ClassName}.{className}";
                var fieldName = GenerateFieldName(tagHelper);

                _tagHelpers.Add(tagHelper, (className, fullyQualifiedName, fieldName));

                return true;
            }

            public string GetClassName(TagHelperDescriptor taghelper)
            {
                return _tagHelpers[taghelper].className;
            }

            public string GetFullyQualifiedName(TagHelperDescriptor taghelper)
            {
                return _tagHelpers[taghelper].fullyQualifiedName;
            }

            public string GetFieldName(TagHelperDescriptor taghelper)
            {
                return _tagHelpers[taghelper].fieldName;
            }

            private static string GenerateFieldName(TagHelperDescriptor tagHelper)
            {
                return $"__{tagHelper.GetViewComponentName()}ViewComponentTagHelper";
            }
        }
    }
}
