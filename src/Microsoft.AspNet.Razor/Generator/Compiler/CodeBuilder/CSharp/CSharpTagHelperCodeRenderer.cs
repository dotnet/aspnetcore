// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    /// <summary>
    /// Renders tag helper rendering code.
    /// </summary>
    public class CSharpTagHelperCodeRenderer
    {
        internal static readonly string ExecutionContextVariableName = "__tagHelperExecutionContext";
        internal static readonly string StringValueBufferVariableName = "__tagHelperStringValueBuffer";
        internal static readonly string ScopeManagerVariableName = "__tagHelperScopeManager";
        internal static readonly string RunnerVariableName = "__tagHelperRunner";

        private static readonly TagHelperAttributeDescriptorComparer AttributeDescriptorComparer =
            new TagHelperAttributeDescriptorComparer();

        private readonly CSharpCodeWriter _writer;
        private readonly CodeBuilderContext _context;
        private readonly IChunkVisitor _bodyVisitor;
        private readonly GeneratedTagHelperContext _tagHelperContext;
        private readonly bool _designTimeMode;

        /// <summary>
        /// Instantiates a new <see cref="CSharpTagHelperCodeRenderer"/>.
        /// </summary>
        /// <param name="bodyVisitor">The <see cref="IChunkVisitor"/> used to render chunks found in the body.</param>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> used to write code.</param>
        /// <param name="context">A <see cref="CodeBuilderContext"/> instance that contains information about
        /// the current code generation process.</param>
        public CSharpTagHelperCodeRenderer([NotNull] IChunkVisitor bodyVisitor,
                                           [NotNull] CSharpCodeWriter writer,
                                           [NotNull] CodeBuilderContext context)
        {
            _bodyVisitor = bodyVisitor;
            _writer = writer;
            _context = context;
            _tagHelperContext = context.Host.GeneratedClassContext.GeneratedTagHelperContext;
            _designTimeMode = context.Host.DesignTimeMode;
            AttributeValueCodeRenderer = new TagHelperAttributeValueCodeRenderer();
        }

        public TagHelperAttributeValueCodeRenderer AttributeValueCodeRenderer { get; set; }

        /// <summary>
        /// Renders the code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">A <see cref="TagHelperChunk"/> to render.</param>
        public void RenderTagHelper(TagHelperChunk chunk)
        {
            var tagHelperDescriptors = chunk.Descriptors;

            RenderBeginTagHelperScope(chunk.TagName, chunk.SelfClosing, chunk.Children);

            RenderTagHelpersCreation(chunk);

            var attributeDescriptors = tagHelperDescriptors.SelectMany(descriptor => descriptor.Attributes);
            var boundHTMLAttributes = attributeDescriptors.Select(descriptor => descriptor.Name);
            var htmlAttributes = chunk.Attributes;
            var unboundHTMLAttributes =
                htmlAttributes.Where(htmlAttribute => !boundHTMLAttributes.Contains(htmlAttribute.Key,
                                                                                    StringComparer.OrdinalIgnoreCase));

            RenderUnboundHTMLAttributes(unboundHTMLAttributes);

            // No need to run anything in design time mode.
            if (!_designTimeMode)
            {
                RenderRunTagHelpers();

                RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName, methodReturnsString: true);
                RenderTagOutput(_tagHelperContext.OutputGeneratePreContentMethodName, methodReturnsString: false);

                RenderTagHelperContent();

                RenderTagOutput(_tagHelperContext.OutputGeneratePostContentMethodName, methodReturnsString: false);
                RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName, methodReturnsString: true);

                RenderEndTagHelpersScope();
            }
        }

        internal static string GetVariableName(TagHelperDescriptor descriptor)
        {
            return "__" + descriptor.TypeName.Replace('.', '_');
        }

        private void RenderBeginTagHelperScope(string tagName, bool selfClosing, IList<Chunk> children)
        {
            // Scopes/execution contexts are a runtime feature.
            if (_designTimeMode)
            {
                // Render all of the tag helper children inline for IntelliSense.
                _bodyVisitor.Accept(children);
                return;
            }

            // Call into the tag helper scope manager to start a new tag helper scope.
            // Also capture the value as the current execution context.
            _writer.WriteStartAssignment(ExecutionContextVariableName)
                   .WriteStartInstanceMethodInvocation(ScopeManagerVariableName,
                                                       _tagHelperContext.ScopeManagerBeginMethodName);

            // Assign a unique ID for this instance of the source HTML tag. This must be unique
            // per call site, e.g. if the tag is on the view twice, there should be two IDs.
            _writer.WriteStringLiteral(tagName)
                   .WriteParameterSeparator()
                   .WriteBooleanLiteral(selfClosing)
                   .WriteParameterSeparator()
                   .WriteStringLiteral(GenerateUniqueId())
                   .WriteParameterSeparator();

            // We remove the target writer so TagHelper authors can retrieve content.
            var oldWriter = _context.TargetWriterName;
            _context.TargetWriterName = null;

            // Disabling instrumentation inside TagHelper bodies since we never know if it's accurate
            var oldInstrumentation = _context.Host.EnableInstrumentation;
            _context.Host.EnableInstrumentation = false;

            using (_writer.BuildAsyncLambda(endLine: false))
            {
                // Render all of the tag helper children.
                _bodyVisitor.Accept(children);
            }

            _context.Host.EnableInstrumentation = oldInstrumentation;

            _context.TargetWriterName = oldWriter;

            _writer.WriteParameterSeparator()
                   .Write(_tagHelperContext.StartTagHelperWritingScopeMethodName)
                   .WriteParameterSeparator()
                   .Write(_tagHelperContext.EndTagHelperWritingScopeMethodName)
                   .WriteEndMethodInvocation();
        }

        /// <summary>
        /// Generates a unique ID for an HTML element.
        /// </summary>
        /// <returns>
        /// A globally unique ID.
        /// </returns>
        protected virtual string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private void RenderTagHelpersCreation(TagHelperChunk chunk)
        {
            var tagHelperDescriptors = chunk.Descriptors;

            // This is to maintain value accessors for attributes when creating the TagHelpers.
            // Ultimately it enables us to do scenarios like this:
            // myTagHelper1.Foo = DateTime.Now;
            // myTagHelper2.Foo = myTagHelper1.Foo;
            var htmlAttributeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tagHelperDescriptor in tagHelperDescriptors)
            {
                var tagHelperVariableName = GetVariableName(tagHelperDescriptor);

                // Create the tag helper
                _writer.WriteStartAssignment(tagHelperVariableName)
                       .WriteStartMethodInvocation(_tagHelperContext.CreateTagHelperMethodName,
                                                   tagHelperDescriptor.TypeName)
                       .WriteEndMethodInvocation();

                // Execution contexts are a runtime feature.
                if (!_designTimeMode)
                {
                    _writer.WriteInstanceMethodInvocation(ExecutionContextVariableName,
                                                          _tagHelperContext.ExecutionContextAddMethodName,
                                                          tagHelperVariableName);
                }

                // Render all of the bound attribute values for the tag helper.
                RenderBoundHTMLAttributes(chunk.Attributes,
                                          tagHelperVariableName,
                                          tagHelperDescriptor.Attributes,
                                          htmlAttributeValues);
            }
        }

        private void RenderBoundHTMLAttributes(IDictionary<string, Chunk> chunkAttributes,
                                               string tagHelperVariableName,
                                               IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
                                               Dictionary<string, string> htmlAttributeValues)
        {
            foreach (var attributeDescriptor in attributeDescriptors)
            {
                Chunk attributeValueChunk;

                var providedAttribute = chunkAttributes.TryGetValue(attributeDescriptor.Name,
                                                                    out attributeValueChunk);

                if (providedAttribute)
                {
                    var attributeValueRecorded = htmlAttributeValues.ContainsKey(attributeDescriptor.Name);

                    // Bufferable attributes are attributes that can have Razor code inside of them.
                    var bufferableAttribute = IsStringAttribute(attributeDescriptor);

                    // Plain text values are non Razor code (@DateTime.Now) values. If an attribute is bufferable it
                    // may be more than just a plain text value, it may also contain Razor code which is why we attempt
                    // to retrieve a plain text value here.
                    string textValue;
                    var isPlainTextValue = TryGetPlainTextValue(attributeValueChunk, out textValue);

                    // If we haven't recorded a value and we need to buffer an attribute value and the value is not
                    // plain text then we need to prepare the value prior to setting it below.
                    if (!attributeValueRecorded && bufferableAttribute && !isPlainTextValue)
                    {
                        BuildBufferedWritingScope(attributeValueChunk);
                    }

                    // We capture the tag helpers property value accessor so we can retrieve it later (if we need to).
                    var valueAccessor = string.Format(CultureInfo.InvariantCulture,
                                                      "{0}.{1}",
                                                      tagHelperVariableName,
                                                      attributeDescriptor.PropertyName);

                    // If we haven't recorded this attribute value before then we need to record its value.
                    if (!attributeValueRecorded)
                    {
                        // We only need to create attribute values once per HTML element (not once per tag helper).
                        // We're saving the value accessor so we can retrieve it later if there are more tag
                        // helpers that need the value.
                        htmlAttributeValues.Add(attributeDescriptor.Name, valueAccessor);

                        if (bufferableAttribute)
                        {
                            _writer.WriteStartAssignment(valueAccessor);

                            if (isPlainTextValue)
                            {
                                // If the attribute is bufferable but has a plain text value that means the value
                                // is a string which needs to be surrounded in quotes.
                                RenderQuotedAttributeValue(textValue, attributeDescriptor);
                            }
                            else
                            {
                                // The value contains more than plain text e.g.
                                // stringAttribute ="Time: @DateTime.Now"
                                RenderBufferedAttributeValue(attributeDescriptor);
                            }

                            _writer.WriteLine(";");
                        }
                        else
                        {
                            // Write out simple assignment for non-string property value. Try to keep the whole
                            // statement together and the #line pragma correct to make debugging possible.
                            using (var lineMapper = new CSharpLineMappingWriter(
                                _writer,
                                attributeValueChunk.Association.Start,
                                _context.SourceFile))
                            {
                                // Place the assignment LHS to align RHS with original attribute value's indentation.
                                // Unfortunately originalIndent is incorrect if original line contains tabs. Unable to
                                // use a CSharpPaddingBuilder because the Association has no Previous node; lost the
                                // original Span sequence when the parse tree was rewritten.
                                var originalIndent = attributeValueChunk.Start.CharacterIndex;
                                var generatedLength = valueAccessor.Length + " = ".Length;
                                var newIndent = originalIndent - generatedLength;
                                if (newIndent > 0)
                                {
                                    _writer.Indent(newIndent);
                                }

                                _writer.WriteStartAssignment(valueAccessor);
                                lineMapper.MarkLineMappingStart();

                                // Write out bare expression for this attribute value. Property is not a string.
                                // So quoting or buffering are not helpful.
                                RenderRawAttributeValue(attributeValueChunk, attributeDescriptor, isPlainTextValue);

                                // End the assignment to the attribute.
                                lineMapper.MarkLineMappingEnd();
                                _writer.WriteLine(";");
                            }
                        }

                        // Execution contexts are a runtime feature.
                        if (_designTimeMode)
                        {
                            continue;
                        }

                        // We need to inform the context of the attribute value.
                        _writer
                            .WriteStartInstanceMethodInvocation(
                                ExecutionContextVariableName,
                                _tagHelperContext.ExecutionContextAddTagHelperAttributeMethodName)
                            .WriteStringLiteral(attributeDescriptor.Name)
                            .WriteParameterSeparator()
                            .Write(valueAccessor)
                            .WriteEndMethodInvocation();
                    }
                    else
                    {
                        // The attribute value has already been recorded, lets retrieve it from the stored value
                        // accessors.
                        _writer
                            .WriteStartAssignment(valueAccessor)
                            .Write(htmlAttributeValues[attributeDescriptor.Name])
                            .WriteLine(";");
                    }
                }
            }
        }

        private void RenderUnboundHTMLAttributes(IEnumerable<KeyValuePair<string, Chunk>> unboundHTMLAttributes)
        {
            // Build out the unbound HTML attributes for the tag builder
            foreach (var htmlAttribute in unboundHTMLAttributes)
            {
                string textValue;
                var attributeValue = htmlAttribute.Value;
                var isPlainTextValue = TryGetPlainTextValue(attributeValue, out textValue);

                // HTML attributes are always strings. So if this value is not plain text i.e. if the value contains
                // C# code, then we need to buffer it.
                if (!isPlainTextValue)
                {
                    BuildBufferedWritingScope(attributeValue);
                }

                // Execution contexts are a runtime feature, therefore no need to add anything to them.
                if (_designTimeMode)
                {
                    continue;
                }

                _writer.WriteStartInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    _tagHelperContext.ExecutionContextAddHtmlAttributeMethodName);
                _writer.WriteStringLiteral(htmlAttribute.Key)
                       .WriteParameterSeparator();

                // If it's a plain text value then we need to surround the value with quotes.
                if (isPlainTextValue)
                {
                    _writer.WriteStringLiteral(textValue);
                }
                else
                {
                    RenderBufferedAttributeValueAccessor(_writer);
                }

                _writer.WriteEndMethodInvocation();
            }
        }

        private void RenderTagHelperContent()
        {
            _writer.Write("if (")
                   .Write(ExecutionContextVariableName)
                   .Write(".")
                   .Write(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .Write(".")
                   .Write(_tagHelperContext.OutputIsContentModifiedPropertyName)
                   .WriteLine(")");

            // At this point in the codegen, TagHelperOutput.Content is set. We need to use this to render the Content 
            // instead of executing the child content
            using (_writer.BuildScope())
            {
                RenderTagOutput(_tagHelperContext.OutputGenerateContentMethodName, methodReturnsString: false);
            }

            _writer.Write("else if (")
                   .Write(ExecutionContextVariableName)
                   .Write(".")
                   .Write(_tagHelperContext.ExecutionContextChildContentRetrievedPropertyName)
                   .WriteLine(")");

            // Render the body of the else if statement, at this point in the codegen the GetChildContentAsync method 
            // was invoked but the TagHelperOutput's Content was not set. Call into GetChildContentAsync to retrieve
            // the cached value of the content so we don't execute the child content twice.
            using (_writer.BuildScope())
            {
                RenderStartWriteMethod();

                _writer.WriteInstanceMethodInvocation(ExecutionContextVariableName,
                                                      _tagHelperContext.ExecutionContextGetChildContentAsyncMethodName,
                                                      endLine: false);

                _writer.Write(".Result")
                       .WriteEndMethodInvocation();
            }

            _writer.WriteLine("else");

            // Render the body of the else statement, at this point in the codegen the GetChildContentAsync method 
            // was not invoked and the TagHelperOutput's Content was not set. Call into ExecuteChildContentAsync to
            // to execute and render child content.
            using (_writer.BuildScope())
            {
                if (!string.IsNullOrEmpty(_context.TargetWriterName))
                {
                    _writer.WriteMethodInvocation(
                        _tagHelperContext.StartTagHelperWritingScopeMethodName,
                        _context.TargetWriterName);
                }

                _writer.WriteInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    _tagHelperContext.ExecutionContextExecuteChildContentAsyncMethodName,
                    endLine: false);
                _writer.WriteLine(".Wait();");

                if (!string.IsNullOrEmpty(_context.TargetWriterName))
                {
                    _writer.WriteMethodInvocation(_tagHelperContext.EndTagHelperWritingScopeMethodName);
                }
            }
        }

        private void RenderEndTagHelpersScope()
        {
            _writer.WriteStartAssignment(ExecutionContextVariableName)
                   .WriteInstanceMethodInvocation(ScopeManagerVariableName,
                                                  _tagHelperContext.ScopeManagerEndMethodName);
        }

        private void RenderTagOutput(string tagOutputMethodName, bool methodReturnsString)
        {
            // If its a string WriteLiteral must be generated.
            if (methodReturnsString)
            {
                CSharpCodeVisitor.RenderPreWriteStart(_writer, _context);
            }
            else
            {
                RenderStartWriteMethod();
            }

            _writer.Write(ExecutionContextVariableName)
                   .Write(".")
                   .Write(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .Write(".")
                   .WriteMethodInvocation(tagOutputMethodName, endLine: false)
                   .WriteEndMethodInvocation();
        }
        private void RenderStartWriteMethod()
        {
            if (!string.IsNullOrEmpty(_context.TargetWriterName))
            {
                _writer
                    .WriteStartMethodInvocation(_context.Host.GeneratedClassContext.WriteToMethodName)
                    .Write(_context.TargetWriterName)
                    .WriteParameterSeparator();
            }
            else
            {
                _writer.WriteStartMethodInvocation(_context.Host.GeneratedClassContext.WriteMethodName);
            }
        }

        private void RenderRunTagHelpers()
        {
            _writer.Write(ExecutionContextVariableName)
                   .Write(".")
                   .WriteStartAssignment(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .WriteStartInstanceMethodInvocation(RunnerVariableName,
                                                       _tagHelperContext.RunnerRunAsyncMethodName);

            _writer.Write(ExecutionContextVariableName)
                   .WriteEndMethodInvocation(endLine: false)
                   .WriteLine(".Result;");
        }

        private void RenderBufferedAttributeValue(TagHelperAttributeDescriptor attributeDescriptor)
        {
            // Pass complexValue: false because variable.ToString() replaces any original complexity in the expression.
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    RenderBufferedAttributeValueAccessor(writer);
                },
                complexValue: false);
        }

        private void RenderRawAttributeValue(
            Chunk attributeValueChunk,
            TagHelperAttributeDescriptor attributeDescriptor,
            bool isPlainTextValue)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    var visitor =
                        new CSharpTagHelperAttributeValueVisitor(writer, _context, attributeDescriptor.TypeName);
                    visitor.Accept(attributeValueChunk);
                },
                complexValue: !isPlainTextValue);
        }

        private void RenderQuotedAttributeValue(string value, TagHelperAttributeDescriptor attributeDescriptor)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    writer.WriteStringLiteral(value);
                },
                complexValue: false);
        }

        private void BuildBufferedWritingScope(Chunk htmlAttributeChunk)
        {
            // Render a buffered writing scope for the html attribute value.
            BuildBufferedWritingScope(new[] { htmlAttributeChunk });
        }

        private void BuildBufferedWritingScope(IList<Chunk> chunks)
        {
            // We're building a writing scope around the provided chunks which captures everything written from the
            // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
            // ensure we capture all content that's written, directly or indirectly.
            var oldWriter = _context.TargetWriterName;
            _context.TargetWriterName = null;

            // Need to disable instrumentation inside of writing scopes, the instrumentation will not detect
            // content written inside writing scopes.
            var oldInstrumentation = _context.Host.EnableInstrumentation;

            try
            {
                _context.Host.EnableInstrumentation = false;

                // Scopes are a runtime feature.
                if (!_designTimeMode)
                {
                    _writer.WriteMethodInvocation(_tagHelperContext.StartTagHelperWritingScopeMethodName);
                }

                _bodyVisitor.Accept(chunks);

                // Scopes are a runtime feature.
                if (!_designTimeMode)
                {
                    _writer.WriteStartAssignment(StringValueBufferVariableName)
                           .WriteMethodInvocation(_tagHelperContext.EndTagHelperWritingScopeMethodName);
                }
            }
            finally
            {
                // Reset instrumentation back to what it was, leaving the writing scope.
                _context.Host.EnableInstrumentation = oldInstrumentation;

                // Reset the writer/buffer back to what it was, leaving the writing scope.
                _context.TargetWriterName = oldWriter;
            }
        }

        private void RenderAttributeValue(TagHelperAttributeDescriptor attributeDescriptor,
                                          Action<CSharpCodeWriter> valueRenderer,
                                          bool complexValue)
        {
            AttributeValueCodeRenderer.RenderAttributeValue(
                attributeDescriptor,
                _writer,
                _context,
                valueRenderer,
                complexValue);
        }

        private void RenderBufferedAttributeValueAccessor(CSharpCodeWriter writer)
        {
            if (_designTimeMode)
            {
                // There is no value buffer in design time mode but we still want to write out a value. We write a
                // value to ensure the tag helper's property type is string.
                writer.Write("string.Empty");
            }
            else
            {
                writer.WriteInstanceMethodInvocation(StringValueBufferVariableName,
                                                     "ToString",
                                                     endLine: false);
            }
        }

        private static bool IsStringAttribute(TagHelperAttributeDescriptor attributeDescriptor)
        {
            return string.Equals(
                attributeDescriptor.TypeName,
                typeof(string).FullName,
                StringComparison.Ordinal);
        }

        private static bool TryGetPlainTextValue(Chunk chunk, out string plainText)
        {
            var chunkBlock = chunk as ChunkBlock;

            plainText = null;

            if (chunkBlock == null || chunkBlock.Children.Count != 1)
            {
                return false;
            }

            var literalChildChunk = chunkBlock.Children[0] as LiteralChunk;

            if (literalChildChunk == null)
            {
                return false;
            }

            plainText = literalChildChunk.Text;

            return true;
        }

        // This class is used to compare tag helper attributes by comparing only the HTML attribute name.
        private class TagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
        {
            public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
            {
                return string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(TagHelperAttributeDescriptor descriptor)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(descriptor.Name);
            }
        }
    }
}