// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorRuntimeCSharpLoweringPhase : RazorCSharpLoweringPhaseBase
    {
        private static string _tagHelperId;

        internal static string GenerateUniqueTagHelperId {
            get
            {
                return _tagHelperId ?? Guid.NewGuid().ToString("N");
            }
            set
            {
                _tagHelperId = value;
            }
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDependency(irDocument);

            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDependency(syntaxTree);

            var renderingContext = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                SourceDocument = codeDocument.Source,
                Options = syntaxTree.Options,
            };
            var visitor = new CSharpRenderer(renderingContext);
            visitor.VisitDocument(irDocument);
            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = renderingContext.Writer.GenerateCode(),
                LineMappings = renderingContext.LineMappings,
                Diagnostics = renderingContext.ErrorSink.Errors
            };

            codeDocument.SetCSharpDocument(csharpDocument);
        }

        private class CSharpRenderer : PageStructureCSharpRenderer
        {
            public CSharpRenderer(CSharpRenderingContext context) : base(context)
            {
            }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                if (!string.IsNullOrEmpty(node.Bytes))
                {
                    Context.Writer
                    .Write("#pragma checksum \"")
                    .Write(node.Filename)
                    .Write("\" \"")
                    .Write(node.Guid)
                    .Write("\" \"")
                    .Write(node.Bytes)
                    .WriteLine("\"");
                }
            }

            public override void VisitCSharpToken(CSharpTokenIRNode node)
            {
                Context.Writer.Write(node.Content);
            }

            public override void VisitHtml(HtmlContentIRNode node)
            {
                const int MaxStringLiteralLength = 1024;

                var charactersConsumed = 0;

                // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
                while (charactersConsumed < node.Content.Length)
                {
                    string textToRender;
                    if (node.Content.Length <= MaxStringLiteralLength)
                    {
                        textToRender = node.Content;
                    }
                    else
                    {
                        var charactersToSubstring = Math.Min(MaxStringLiteralLength, node.Content.Length - charactersConsumed);
                        textToRender = node.Content.Substring(charactersConsumed, charactersToSubstring);
                    }

                    Context.Writer
                        .Write(Context.RenderingConventions.StartWriteLiteralMethod)
                        .WriteStringLiteral(textToRender)
                        .WriteEndMethodInvocation();

                    charactersConsumed += textToRender.Length;
                }
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                IDisposable linePragmaScope = null;
                if (node.SourceRange != null)
                {
                    linePragmaScope = new LinePragmaWriter(Context.Writer, node.SourceRange);
                    var padding = BuildOffsetPadding(Context.RenderingConventions.StartWriteMethod.Length, node.SourceRange, Context);
                    Context.Writer.Write(padding);
                }

                Context.Writer.Write(Context.RenderingConventions.StartWriteMethod);

                VisitDefault(node);

                Context.Writer.WriteEndMethodInvocation();

                linePragmaScope?.Dispose();
            }

            public override void VisitUsingStatement(UsingStatementIRNode node)
            {
                Context.Writer.WriteUsing(node.Content);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
            {
                var valuePieceCount = node
                    .Children
                    .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var suffixLocation = node.SourceRange.AbsoluteIndex + node.SourceRange.ContentLength - node.Suffix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartBeginWriteAttributeMethod)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Suffix)
                    .WriteParameterSeparator()
                    .Write(suffixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteEndMethodInvocation();

                VisitDefault(node);

                Context.Writer
                    .Write(Context.RenderingConventions.StartEndWriteAttributeMethod)
                    .WriteEndMethodInvocation();
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
            {
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Content)
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(true)
                    .WriteEndMethodInvocation();
            }

            public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
            {
                const string ValueWriterName = "__razor_attribute_value_writer";

                var expressionValue = node.Children.FirstOrDefault() as CSharpExpressionIRNode;
                var linePragma = expressionValue != null ? new LinePragmaWriter(Context.Writer, node.SourceRange) : null;
                var prefixLocation = node.SourceRange.AbsoluteIndex;
                var valueLocation = node.SourceRange.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.SourceRange.ContentLength - node.Prefix.Length;
                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator();

                if (expressionValue != null)
                {
                    Debug.Assert(node.Children.Count == 1);

                    RenderExpressionInline(expressionValue, Context);
                }
                else
                {
                    // Not an expression; need to buffer the result.
                    Context.Writer.WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                    var initialRenderingConventions = Context.RenderingConventions;
                    Context.RenderingConventions = new CSharpRedirectRenderingConventions(ValueWriterName, Context.Writer);
                    using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                    {
                        VisitDefault(node);
                    }
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer.WriteEndMethodInvocation(false);
                }

                Context.Writer
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(false)
                    .WriteEndMethodInvocation();

                linePragma?.Dispose();
            }

            public override void VisitCSharpStatement(CSharpStatementIRNode node)
            {
                if (string.IsNullOrWhiteSpace(node.Content))
                {
                    return;
                }

                if (node.SourceRange != null)
                {
                    using (new LinePragmaWriter(Context.Writer, node.SourceRange))
                    {
                        var padding = BuildOffsetPadding(0, node.SourceRange, Context);
                        Context.Writer
                            .Write(padding)
                            .WriteLine(node.Content);
                    }
                }
                else
                {
                    Context.Writer.WriteLine(node.Content);
                }
            }

            public override void VisitTemplate(TemplateIRNode node)
            {
                const string ItemParameterName = "item";
                const string TemplateWriterName = "__razor_template_writer";

                Context.Writer
                    .Write(ItemParameterName).Write(" => ")
                    .WriteStartNewObject("HelperResult" /* ORIGINAL: TemplateTypeName */);

                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new CSharpRedirectRenderingConventions(TemplateWriterName, Context.Writer);
                using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
                {
                    VisitDefault(node);
                }
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer.WriteEndMethodInvocation(endLine: false);
            }

            internal override void VisitTagHelper(TagHelperIRNode node)
            {
                var initialTagHelperRenderingContext = Context.TagHelperRenderingContext;
                Context.TagHelperRenderingContext = new TagHelperRenderingContext();
                VisitDefault(node);
                Context.TagHelperRenderingContext = initialTagHelperRenderingContext;
            }

            internal override void VisitInitializeTagHelperStructure(InitializeTagHelperStructureIRNode node)
            {
                // Call into the tag helper scope manager to start a new tag helper scope.
                // Also capture the value as the current execution context.
                Context.Writer
                    .WriteStartAssignment("__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */)
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperScopeManager" /* ORIGINAL: ScopeManagerVariableName */,
                        "Begin" /* ORIGINAL: ScopeManagerBeginMethodName */);

                // Assign a unique ID for this instance of the source HTML tag. This must be unique
                // per call site, e.g. if the tag is on the view twice, there should be two IDs.
                Context.Writer.WriteStringLiteral(node.TagName)
                    .WriteParameterSeparator()
                    .Write("global::")
                    .Write(typeof(TagMode).FullName)
                    .Write(".")
                    .Write(node.TagMode.ToString())
                    .WriteParameterSeparator()
                    .WriteStringLiteral(GenerateUniqueTagHelperId)
                    .WriteParameterSeparator();

                // We remove and redirect writers so TagHelper authors can retrieve content.
                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new CSharpRenderingConventions(Context.Writer);
                using (Context.Writer.BuildAsyncLambda(endLine: false))
                {
                    VisitDefault(node);
                }
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer.WriteEndMethodInvocation();
            }

            internal override void VisitCreateTagHelper(CreateTagHelperIRNode node)
            {
                var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

                Context.Writer
                    .WriteStartAssignment(tagHelperVariableName)
                    .WriteStartMethodInvocation(
                        "CreateTagHelper" /* ORIGINAL: CreateTagHelperMethodName */,
                        "global::" + node.TagHelperTypeName)
                    .WriteEndMethodInvocation();

                Context.Writer.WriteInstanceMethodInvocation(
                    "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                    "Add" /* ORIGINAL: ExecutionContextAddMethodName */,
                    tagHelperVariableName);
            }

            internal override void VisitAddPreallocatedTagHelperHtmlAttribute(AddPreallocatedTagHelperHtmlAttributeIRNode node)
            {
                Context.Writer
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                        "AddHtmlAttribute" /* ORIGINAL: ExecutionContextAddHtmlAttributeMethodName */)
                    .Write(node.VariableName)
                    .WriteEndMethodInvocation();
            }

            internal override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
            {
                var attributeValueStyleParameter = $"global::{typeof(HtmlAttributeValueStyle).FullName}.{node.ValueStyle}";
                var isConditionalAttributeValue = node.Children.Any(child => child is CSharpAttributeValueIRNode);

                // All simple text and minimized attributes will be pre-allocated.
                if (isConditionalAttributeValue)
                {
                    // Dynamic attribute value should be run through the conditional attribute removal system. It's
                    // unbound and contains C#.

                    // TagHelper attribute rendering is buffered by default. We do not want to write to the current
                    // writer.
                    var valuePieceCount = node.Children.Count(
                        child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);

                    Context.Writer
                        .WriteStartMethodInvocation("BeginAddHtmlAttributeValues" /* ORIGINAL: BeginAddHtmlAttributeValuesMethodName */)
                        .Write("__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(node.Name)
                        .WriteParameterSeparator()
                        .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                        .WriteParameterSeparator()
                        .Write(attributeValueStyleParameter)
                        .WriteEndMethodInvocation();

                    var initialRenderingConventions = Context.RenderingConventions;
                    Context.RenderingConventions = new TagHelperHtmlAttributeRenderingConventions(Context.Writer);
                    VisitDefault(node);
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer
                        .WriteMethodInvocation(
                            "EndAddHtmlAttributeValues" /* ORIGINAL: EndAddHtmlAttributeValuesMethodName */,
                            "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */);
                }
                else
                {
                    // This is a data-* attribute which includes C#. Do not perform the conditional attribute removal or
                    // other special cases used when IsDynamicAttributeValue(). But the attribute must still be buffered to
                    // determine its final value.

                    // Attribute value is not plain text, must be buffered to determine its final value.
                    Context.Writer.WriteMethodInvocation("BeginWriteTagHelperAttribute" /* ORIGINAL: BeginWriteTagHelperAttributeMethodName */);

                    // We're building a writing scope around the provided chunks which captures everything written from the
                    // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
                    // ensure we capture all content that's written, directly or indirectly.
                    var initialRenderingConventions = Context.RenderingConventions;
                    Context.RenderingConventions = new CSharpRenderingConventions(Context.Writer);
                    VisitDefault(node);
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer
                        .WriteStartAssignment("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                        .WriteMethodInvocation("EndWriteTagHelperAttribute" /* ORIGINAL: EndWriteTagHelperAttributeMethodName */)
                        .WriteStartInstanceMethodInvocation(
                            "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                            "AddHtmlAttribute" /* ORIGINAL: ExecutionContextAddHtmlAttributeMethodName */)
                        .WriteStringLiteral(node.Name)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation("Html.Raw" /* ORIGINAL: MarkAsHtmlEncodedMethodName */)
                        .Write("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                        .WriteEndMethodInvocation(endLine: false)
                        .WriteParameterSeparator()
                        .Write(attributeValueStyleParameter)
                        .WriteEndMethodInvocation();
                }
            }

            internal override void VisitSetPreallocatedTagHelperProperty(SetPreallocatedTagHelperPropertyIRNode node)
            {
                var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
                var propertyValueAccessor = GetTagHelperPropertyAccessor(tagHelperVariableName, node.AttributeName, node.Descriptor);
                var attributeValueAccessor = $"{node.VariableName}.Value" /* ORIGINAL: TagHelperAttributeValuePropertyName */;
                Context.Writer
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write("(string)")
                    .Write(attributeValueAccessor)
                    .WriteLine(";")
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                        "AddTagHelperAttribute" /* ORIGINAL: ExecutionContextAddTagHelperAttributeMethodName */)
                    .Write(node.VariableName)
                    .WriteEndMethodInvocation();
            }

            internal override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
                var tagHelperRenderingContext = Context.TagHelperRenderingContext;

                // Ensure that the property we're trying to set has initialized its dictionary bound properties.
                if (node.Descriptor.IsIndexer &&
                    tagHelperRenderingContext.VerifiedPropertyDictionaries.Add(node.Descriptor.PropertyName))
                {
                    // Throw a reasonable Exception at runtime if the dictionary property is null.
                    Context.Writer
                        .Write("if (")
                        .Write(tagHelperVariableName)
                        .Write(".")
                        .Write(node.Descriptor.PropertyName)
                        .WriteLine(" == null)");
                    using (Context.Writer.BuildScope())
                    {
                        // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                        // of InvalidOperationException type.
                        Context.Writer
                            .Write("throw ")
                            .WriteStartNewObject(nameof(InvalidOperationException))
                            .WriteStartMethodInvocation("FormatInvalidIndexerAssignment" /* ORIGINAL: FormatInvalidIndexerAssignmentMethodName */)
                            .WriteStringLiteral(node.AttributeName)
                            .WriteParameterSeparator()
                            .WriteStringLiteral(node.TagHelperTypeName)
                            .WriteParameterSeparator()
                            .WriteStringLiteral(node.Descriptor.PropertyName)
                            .WriteEndMethodInvocation(endLine: false)   // End of method call
                            .WriteEndMethodInvocation();   // End of new expression / throw statement
                    }
                }

                var propertyValueAccessor = GetTagHelperPropertyAccessor(tagHelperVariableName, node.AttributeName, node.Descriptor);

                string previousValueAccessor;
                if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out previousValueAccessor))
                {
                    Context.Writer
                        .WriteStartAssignment(propertyValueAccessor)
                        .Write(previousValueAccessor)
                        .WriteLine(";");

                    return;
                }
                else
                {
                    tagHelperRenderingContext.RenderedBoundAttributes[node.AttributeName] = propertyValueAccessor;
                }

                if (node.Descriptor.IsStringProperty)
                {
                    Context.Writer.WriteMethodInvocation("BeginWriteTagHelperAttribute" /* ORIGINAL: BeginWriteTagHelperAttributeMethodName */);

                    var initialRenderingConventions = Context.RenderingConventions;
                    Context.RenderingConventions = new CSharpLiteralCodeConventions(Context.Writer);
                    VisitDefault(node);
                    Context.RenderingConventions = initialRenderingConventions;

                    Context.Writer
                        .WriteStartAssignment("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                        .WriteMethodInvocation("EndWriteTagHelperAttribute" /* ORIGINAL: EndWriteTagHelperAttributeMethodName */)
                        .WriteStartAssignment(propertyValueAccessor)
                        .Write("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                        .WriteLine(";");
                }
                else
                {
                    using (new LinePragmaWriter(Context.Writer, node.SourceRange))
                    {
                        Context.Writer.WriteStartAssignment(propertyValueAccessor);

                        if (node.Descriptor.IsEnum &&
                            node.Children.Count == 1 &&
                            node.Children.First() is HtmlContentIRNode)
                        {
                            Context.Writer
                                .Write("global::")
                                .Write(node.Descriptor.TypeName)
                                .Write(".");
                        }

                        RenderTagHelperAttributeInline(node, node.SourceRange);

                        Context.Writer.WriteLine(";");
                    }
                }

                // We need to inform the context of the attribute value.
                Context.Writer
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                        "AddTagHelperAttribute" /* ORIGINAL: ExecutionContextAddTagHelperAttributeMethodName */)
                    .WriteStringLiteral(node.AttributeName)
                    .WriteParameterSeparator()
                    .Write(propertyValueAccessor)
                    .WriteParameterSeparator()
                    .Write($"global::{typeof(HtmlAttributeValueStyle).FullName}.{node.ValueStyle}")
                    .WriteEndMethodInvocation();
            }

            internal override void VisitExecuteTagHelpers(ExecuteTagHelpersIRNode node)
            {
                Context.Writer
                    .Write("await ")
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperRunner" /* ORIGINAL: RunnerVariableName */,
                        "RunAsync" /* ORIGINAL: RunnerRunAsyncMethodName */)
                    .Write("__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */)
                    .WriteEndMethodInvocation();

                var executionContextVariableName = "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */;
                var executionContextOutputPropertyName = "Output" /* ORIGINAL: ExecutionContextOutputPropertyName */;
                var tagHelperOutputAccessor = $"{executionContextVariableName}.{executionContextOutputPropertyName}";

                Context.Writer
                    .Write("if (!")
                    .Write(tagHelperOutputAccessor)
                    .Write(".")
                    .Write("IsContentModified" /* ORIGINAL: TagHelperOutputIsContentModifiedPropertyName */)
                    .WriteLine(")");

                using (Context.Writer.BuildScope())
                {
                    Context.Writer
                        .Write("await ")
                        .WriteInstanceMethodInvocation(
                            executionContextVariableName,
                            "SetOutputContentAsync" /* ORIGINAL: ExecutionContextSetOutputContentAsyncMethodName */);
                }

                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteMethod)
                    .Write(tagHelperOutputAccessor)
                    .WriteEndMethodInvocation()
                    .WriteStartAssignment(executionContextVariableName)
                    .WriteInstanceMethodInvocation(
                        "__tagHelperScopeManager" /* ORIGINAL: ScopeManagerVariableName */,
                        "End" /* ORIGINAL: ScopeManagerEndMethodName */);
            }

            internal override void VisitDeclarePreallocatedTagHelperHtmlAttribute(DeclarePreallocatedTagHelperHtmlAttributeIRNode node)
            {
                Context.Writer
                    .Write("private static readonly global::")
                    .Write("Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                    .Write(" ")
                    .Write(node.VariableName)
                    .Write(" = ")
                    .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                    .WriteStringLiteral(node.Name);

                if (node.ValueStyle == HtmlAttributeValueStyle.Minimized)
                {
                    Context.Writer.WriteEndMethodInvocation();
                }
                else
                {
                    Context.Writer
                        .WriteParameterSeparator()
                        .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Html.HtmlString" /* ORIGINAL: EncodedHtmlStringTypeName */)
                        .WriteStringLiteral(node.Value)
                        .WriteEndMethodInvocation(endLine: false)
                        .WriteParameterSeparator()
                        .Write($"global::{typeof(HtmlAttributeValueStyle).FullName}.{node.ValueStyle}")
                        .WriteEndMethodInvocation();
                }
            }

            internal override void VisitDeclarePreallocatedTagHelperAttribute(DeclarePreallocatedTagHelperAttributeIRNode node)
            {
                Context.Writer
                    .Write("private static readonly global::")
                    .Write("Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                    .Write(" ")
                    .Write(node.VariableName)
                    .Write(" = ")
                    .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Value)
                    .WriteParameterSeparator()
                    .Write($"global::{typeof(HtmlAttributeValueStyle).FullName}.{node.ValueStyle}")
                    .WriteEndMethodInvocation();
            }

            internal override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
            {
                Context.Writer.WriteLineHiddenDirective();

                // Need to disable the warning "X is assigned to but never used." for the value buffer since
                // whether it's used depends on how a TagHelper is used.
                Context.Writer
                    .WritePragma("warning disable 0414")
                    .Write("private ")
                    .WriteVariableDeclaration("string", "__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */, value: null)
                    .WritePragma("warning restore 0414");

                Context.Writer
                .Write("private global::")
                .WriteVariableDeclaration(
                    "Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext" /* ORIGINAL: ExecutionContextTypeName */,
                    "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                    value: null);

                Context.Writer
                .Write("private global::")
                .Write("Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner" /* ORIGINAL: RunnerTypeName */)
                .Write(" ")
                .Write("__tagHelperRunner" /* ORIGINAL: RunnerVariableName */)
                .Write(" = new global::")
                .Write("Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner" /* ORIGINAL: RunnerTypeName */)
                .WriteLine("();");

                const string backedScopeManageVariableName = "__backed" + "__tagHelperScopeManager" /* ORIGINAL: ScopeManagerVariableName */;
                Context.Writer
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        "Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager",
                        backedScopeManageVariableName,
                        value: null);

                Context.Writer
                .Write("private global::")
                .Write("Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager" /* ORIGINAL: ScopeManagerTypeName */)
                .Write(" ")
                .WriteLine("__tagHelperScopeManager" /* ORIGINAL: ScopeManagerVariableName */);

                using (Context.Writer.BuildScope())
                {
                    Context.Writer.WriteLine("get");
                    using (Context.Writer.BuildScope())
                    {
                        Context.Writer
                            .Write("if (")
                            .Write(backedScopeManageVariableName)
                            .WriteLine(" == null)");

                        using (Context.Writer.BuildScope())
                        {
                            Context.Writer
                                .WriteStartAssignment(backedScopeManageVariableName)
                                .WriteStartNewObject("Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager" /* ORIGINAL: ScopeManagerTypeName */)
                                .Write("StartTagHelperWritingScope" /* ORIGINAL: StartTagHelperWritingScopeMethodName */)
                                .WriteParameterSeparator()
                                .Write("EndTagHelperWritingScope" /* ORIGINAL: EndTagHelperWritingScopeMethodName */)
                                .WriteEndMethodInvocation();
                        }

                        Context.Writer.WriteReturn(backedScopeManageVariableName);
                    }
                }

                foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
                {
                    var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                    Context.Writer
                        .Write("private global::")
                        .WriteVariableDeclaration(
                            tagHelperTypeName,
                            tagHelperVariableName,
                            value: null);
                }
            }

            private void RenderTagHelperAttributeInline(
                RazorIRNode node,
                MappingLocation documentLocation)
            {
                if (node is SetTagHelperPropertyIRNode || node is CSharpExpressionIRNode)
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        RenderTagHelperAttributeInline(node.Children[i], documentLocation);
                    }
                }
                else if (node is HtmlContentIRNode)
                {
                    Context.Writer.Write(((HtmlContentIRNode)node).Content);
                }
                else if (node is CSharpTokenIRNode)
                {
                    Context.Writer.Write(((CSharpTokenIRNode)node).Content);
                }
                else if (node is CSharpStatementIRNode)
                {
                    Context.ErrorSink.OnError(
                        new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.ContentLength),
                        LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                        documentLocation.ContentLength);
                }
                else if (node is TemplateIRNode)
                {
                    var attributeValueNode = (SetTagHelperPropertyIRNode)node.Parent;
                    Context.ErrorSink.OnError(
                        new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.ContentLength),
                        LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(attributeValueNode.Descriptor.TypeName),
                        documentLocation.ContentLength);
                }
            }
        }
    }
}