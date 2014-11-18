// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            // Find the first content behavior that doesn't have a content behavior of None.
            // The resolver restricts content behavior collisions so the first one that's not None will be
            // the content behavior we need to abide by. None can work in unison with other ContentBehaviors.
            var contentBehavior = tagHelperDescriptors.Select(descriptor => descriptor.ContentBehavior)
                                                      .FirstOrDefault(
                                                            behavior => behavior != ContentBehavior.None);

            RenderBeginTagHelperScope(chunk.TagName);

            RenderTagHelpersCreation(chunk);

            var attributeDescriptors = tagHelperDescriptors.SelectMany(descriptor => descriptor.Attributes);
            var boundHTMLAttributes = attributeDescriptors.Select(descriptor => descriptor.Name);
            var htmlAttributes = chunk.Attributes;
            var unboundHTMLAttributes =
                htmlAttributes.Where(htmlAttribute => !boundHTMLAttributes.Contains(htmlAttribute.Key,
                                                                                    StringComparer.OrdinalIgnoreCase));

            RenderUnboundHTMLAttributes(unboundHTMLAttributes);

            switch (contentBehavior)
            {
                case ContentBehavior.None:
                    RenderRunTagHelpers(bufferedBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName);
                    RenderTagHelperBody(chunk.Children, bufferBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName);
                    break;
                case ContentBehavior.Append:
                    RenderRunTagHelpers(bufferedBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName);
                    RenderTagHelperBody(chunk.Children, bufferBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateContentMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName);
                    break;
                case ContentBehavior.Prepend:
                    RenderRunTagHelpers(bufferedBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateContentMethodName);
                    RenderTagHelperBody(chunk.Children, bufferBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName);
                    break;
                case ContentBehavior.Replace:
                    RenderRunTagHelpers(bufferedBody: false);
                    RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateContentMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName);
                    break;
                case ContentBehavior.Modify:
                    RenderTagHelperBody(chunk.Children, bufferBody: true);
                    RenderRunTagHelpers(bufferedBody: true);
                    RenderTagOutput(_tagHelperContext.OutputGenerateStartTagMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateContentMethodName);
                    RenderTagOutput(_tagHelperContext.OutputGenerateEndTagMethodName);
                    break;
            }

            RenderEndTagHelpersScope();
        }

        internal static string GetVariableName(TagHelperDescriptor descriptor)
        {
            return "__" + descriptor.TypeName.Replace('.', '_');
        }

        private void RenderBeginTagHelperScope(string tagName)
        {
            // Scopes/execution contexts are a runtime feature.
            if (_designTimeMode)
            {
                return;
            }

            // Call into the tag helper scope manager to start a new tag helper scope.
            // Also capture the value as the current execution context.
            _writer.WriteStartAssignment(ExecutionContextVariableName)
                   .WriteStartInstanceMethodInvocation(ScopeManagerVariableName,
                                                       _tagHelperContext.ScopeManagerBeginMethodName);
            _writer.WriteStringLiteral(tagName)
                   .WriteEndMethodInvocation();
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

                    _writer.WriteStartAssignment(valueAccessor);

                    // If we haven't recorded this attribute value before then we need to record its value.
                    if (!attributeValueRecorded)
                    {
                        // We only need to create attribute values once per HTML element (not once per tag helper).
                        // We're saving the value accessor so we can retrieve it later if there are more tag helpers that
                        // need the value.
                        htmlAttributeValues.Add(attributeDescriptor.Name, valueAccessor);

                        if (bufferableAttribute)
                        {
                            // If the attribute is bufferable but has a plain text value that means the value
                            // is a string which needs to be surrounded in quotes.
                            if (isPlainTextValue)
                            {
                                RenderQuotedAttributeValue(textValue, attributeDescriptor);
                            }
                            else
                            {
                                // The value contains more than plain text. e.g. someAttribute="Time: @DateTime.Now"
                                RenderBufferedAttributeValue(attributeDescriptor);
                            }
                        }
                        else
                        {
                            // TODO: Make complex types in non-bufferable attributes work in
                            // https://github.com/aspnet/Razor/issues/129
                            if (!isPlainTextValue)
                            {
                                _writer.WriteLine(";");
                                return;
                            }

                            // We aren't a bufferable attribute which means we have no Razor code in our value.
                            // Therefore we can just use the "textValue" as the attribute value.
                            RenderRawAttributeValue(textValue, attributeDescriptor);
                        }

                        // End the assignment to the attribute.
                        _writer.WriteLine(";");

                        // Execution contexts are a runtime feature.
                        if (_designTimeMode)
                        {
                            continue;
                        }

                        // We need to inform the context of the attribute value.
                        _writer.WriteStartInstanceMethodInvocation(
                            ExecutionContextVariableName,
                            _tagHelperContext.ExecutionContextAddTagHelperAttributeMethodName);

                        _writer.WriteStringLiteral(attributeDescriptor.Name)
                               .WriteParameterSeparator()
                               .Write(valueAccessor)
                               .WriteEndMethodInvocation();
                    }
                    else
                    {
                        // The attribute value has already been recorded, lets retrieve it from the stored value accessors.
                        _writer.Write(htmlAttributeValues[attributeDescriptor.Name])
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

        private void RenderTagHelperBody(IList<Chunk> children, bool bufferBody)
        {
            // If we want to buffer the body we need to create a writing scope to capture the body content.
            if (bufferBody)
            {
                // Render all of the tag helper children in a buffered writing scope.
                BuildBufferedWritingScope(children);
            }
            else
            {
                // Render all of the tag helper children.
                _bodyVisitor.Accept(children);
            }
        }

        private void RenderEndTagHelpersScope()
        {
            // Scopes/execution contexts are a runtime feature.
            if (_designTimeMode)
            {
                return;
            }

            _writer.WriteStartAssignment(ExecutionContextVariableName)
                   .WriteInstanceMethodInvocation(ScopeManagerVariableName,
                                                  _tagHelperContext.ScopeManagerEndMethodName);
        }

        private void RenderTagOutput(string tagOutputMethodName)
        {
            // Rendering output is a runtime feature.
            if (_designTimeMode)
            {
                return;
            }

            CSharpCodeVisitor.RenderPreWriteStart(_writer, _context);

            _writer.Write(ExecutionContextVariableName)
                   .Write(".")
                   .Write(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .Write(".")
                   .WriteMethodInvocation(tagOutputMethodName, endLine: false)
                   .WriteEndMethodInvocation();
        }

        private void RenderRunTagHelpers(bool bufferedBody)
        {
            // No need to run anything in design time mode.
            if (_designTimeMode)
            {
                return;
            }

            _writer.Write(ExecutionContextVariableName)
                   .Write(".")
                   .Write(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .Write(" = ")
                   .WriteStartInstanceMethodInvocation(RunnerVariableName,
                                                       _tagHelperContext.RunnerRunAsyncMethodName);
            _writer.Write(ExecutionContextVariableName);

            if (bufferedBody)
            {
                _writer.WriteParameterSeparator()
                       .Write(StringValueBufferVariableName);
            }

            _writer.WriteEndMethodInvocation(endLine: false)
                   .WriteLine(".Result;");
        }

        private void RenderBufferedAttributeValue(TagHelperAttributeDescriptor attributeDescriptor)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    RenderBufferedAttributeValueAccessor(writer);
                });
        }

        private void RenderRawAttributeValue(string value, TagHelperAttributeDescriptor attributeDescriptor)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    writer.Write(value);
                });
        }

        private void RenderQuotedAttributeValue(string value, TagHelperAttributeDescriptor attributeDescriptor)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    writer.WriteStringLiteral(value);
                });
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
                    _writer.WriteMethodInvocation(_tagHelperContext.StartWritingScopeMethodName);
                }

                _bodyVisitor.Accept(chunks);

                // Scopes are a runtime feature.
                if (!_designTimeMode)
                {
                    _writer.WriteStartAssignment(StringValueBufferVariableName)
                           .WriteMethodInvocation(_tagHelperContext.EndWritingScopeMethodName);
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
                                          Action<CSharpCodeWriter> valueRenderer)
        {
            AttributeValueCodeRenderer.RenderAttributeValue(attributeDescriptor, _writer, _context, valueRenderer);
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