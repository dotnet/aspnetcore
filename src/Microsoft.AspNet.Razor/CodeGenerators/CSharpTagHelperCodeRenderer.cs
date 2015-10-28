// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNet.Razor.CodeGenerators
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

        private readonly CSharpCodeWriter _writer;
        private readonly CodeGeneratorContext _context;
        private readonly IChunkVisitor _bodyVisitor;
        private readonly IChunkVisitor _literalBodyVisitor;
        private readonly TagHelperAttributeCodeVisitor _attributeCodeVisitor;
        private readonly GeneratedTagHelperContext _tagHelperContext;
        private readonly bool _designTimeMode;

        /// <summary>
        /// Instantiates a new <see cref="CSharpTagHelperCodeRenderer"/>.
        /// </summary>
        /// <param name="bodyVisitor">The <see cref="IChunkVisitor"/> used to render chunks found in the body.</param>
        /// <param name="writer">The <see cref="CSharpCodeWriter"/> used to write code.</param>
        /// <param name="context">A <see cref="CodeGeneratorContext"/> instance that contains information about
        /// the current code generation process.</param>
        public CSharpTagHelperCodeRenderer(
            IChunkVisitor bodyVisitor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (bodyVisitor == null)
            {
                throw new ArgumentNullException(nameof(bodyVisitor));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _bodyVisitor = bodyVisitor;
            _writer = writer;
            _context = context;
            _tagHelperContext = context.Host.GeneratedClassContext.GeneratedTagHelperContext;
            _designTimeMode = context.Host.DesignTimeMode;

            _literalBodyVisitor = new CSharpLiteralCodeVisitor(this, writer, context);
            _attributeCodeVisitor = new TagHelperAttributeCodeVisitor(writer, context);
            AttributeValueCodeRenderer = new TagHelperAttributeValueCodeRenderer();
        }

        public TagHelperAttributeValueCodeRenderer AttributeValueCodeRenderer { get; set; }

        /// <summary>
        /// Renders the code for the given <paramref name="chunk"/>.
        /// </summary>
        /// <param name="chunk">A <see cref="TagHelperChunk"/> to render.</param>
        public void RenderTagHelper(TagHelperChunk chunk)
        {
            // Remove any duplicate TagHelperDescriptors that reference the same type name. Duplicates can occur when
            // multiple HtmlTargetElement attributes are on a TagHelper type and matches overlap for an HTML element.
            // Having more than one descriptor with the same TagHelper type results in generated code that runs
            // the same TagHelper X many times (instead of once) over a single HTML element.
            var tagHelperDescriptors = chunk.Descriptors.Distinct(TypeBasedTagHelperDescriptorComparer.Default);

            RenderBeginTagHelperScope(chunk.TagName, chunk.TagMode, chunk.Children);

            RenderTagHelpersCreation(chunk, tagHelperDescriptors);

            RenderAttributes(chunk.Attributes, tagHelperDescriptors);

            // No need to run anything in design time mode.
            if (!_designTimeMode)
            {
                RenderRunTagHelpers();
                RenderWriteTagHelperMethodCall(chunk);
                RenderEndTagHelpersScope();
            }
        }

        internal static string GetVariableName(TagHelperDescriptor descriptor)
        {
            return "__" + descriptor.TypeName.Replace('.', '_');
        }

        private void RenderBeginTagHelperScope(string tagName, TagMode tagMode, IList<Chunk> children)
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
            _writer
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteStartInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    _tagHelperContext.ScopeManagerBeginMethodName);

            // Assign a unique ID for this instance of the source HTML tag. This must be unique
            // per call site, e.g. if the tag is on the view twice, there should be two IDs.
            _writer.WriteStringLiteral(tagName)
                   .WriteParameterSeparator()
                   .Write("global::")
                   .Write(typeof(TagMode).FullName)
                   .Write(".")
                   .Write(tagMode.ToString())
                   .WriteParameterSeparator()
                   .WriteStringLiteral(GenerateUniqueId())
                   .WriteParameterSeparator();

            // We remove the target writer so TagHelper authors can retrieve content.
            var oldWriter = _context.TargetWriterName;
            _context.TargetWriterName = null;

            using (_writer.BuildAsyncLambda(endLine: false))
            {
                // Render all of the tag helper children.
                _bodyVisitor.Accept(children);
            }

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

        private void RenderTagHelpersCreation(
            TagHelperChunk chunk,
            IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
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
                       .WriteStartMethodInvocation(
                            _tagHelperContext.CreateTagHelperMethodName,
                            "global::" + tagHelperDescriptor.TypeName)
                       .WriteEndMethodInvocation();

                // Execution contexts and throwing errors for null dictionary properties are a runtime feature.
                if (_designTimeMode)
                {
                    continue;
                }

                _writer.WriteInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    _tagHelperContext.ExecutionContextAddMethodName,
                    tagHelperVariableName);

                // Track dictionary properties we have confirmed are non-null.
                var confirmedDictionaries = new HashSet<string>(StringComparer.Ordinal);

                // Ensure that all created TagHelpers have initialized dictionary bound properties which are used
                // via TagHelper indexers.
                foreach (var chunkAttribute in chunk.Attributes)
                {
                    var associatedAttributeDescriptor = tagHelperDescriptor.Attributes.FirstOrDefault(
                        attributeDescriptor => attributeDescriptor.IsNameMatch(chunkAttribute.Key));

                    if (associatedAttributeDescriptor != null &&
                        associatedAttributeDescriptor.IsIndexer &&
                        confirmedDictionaries.Add(associatedAttributeDescriptor.PropertyName))
                    {
                        // Throw a reasonable Exception at runtime if the dictionary property is null.
                        _writer
                            .Write("if (")
                            .Write(tagHelperVariableName)
                            .Write(".")
                            .Write(associatedAttributeDescriptor.PropertyName)
                            .WriteLine(" == null)");
                        using (_writer.BuildScope())
                        {
                            // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                            // of InvalidOperationException type.
                            _writer
                                .Write("throw ")
                                .WriteStartNewObject(nameof(InvalidOperationException))
                                .WriteStartMethodInvocation(_tagHelperContext.FormatInvalidIndexerAssignmentMethodName)
                                .WriteStringLiteral(chunkAttribute.Key)
                                .WriteParameterSeparator()
                                .WriteStringLiteral(tagHelperDescriptor.TypeName)
                                .WriteParameterSeparator()
                                .WriteStringLiteral(associatedAttributeDescriptor.PropertyName)
                                .WriteEndMethodInvocation(endLine: false)   // End of method call
                                .WriteEndMethodInvocation(endLine: true);   // End of new expression / throw statement
                        }
                    }
                }
            }
        }

        private void RenderAttributes(
            IList<KeyValuePair<string, Chunk>> chunkAttributes,
            IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            var renderedBoundAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Go through the HTML attributes in source order, assigning to properties or indexers or adding to
            // TagHelperExecutionContext.HTMLAttributes' as we go.
            foreach (var attribute in chunkAttributes)
            {
                var attributeName = attribute.Key;
                var attributeValueChunk = attribute.Value;
                var associatedDescriptors = tagHelperDescriptors.Where(descriptor =>
                    descriptor.Attributes.Any(attributeDescriptor => attributeDescriptor.IsNameMatch(attributeName)));

                // Bound attributes have associated descriptors. First attribute value wins if there are duplicates;
                // later values of duplicate bound attributes are treated as if they were unbound.
                if (associatedDescriptors.Any() && renderedBoundAttributeNames.Add(attributeName))
                {
                    if (attributeValueChunk == null)
                    {
                        // Minimized attributes are not valid for bound attributes. TagHelperBlockRewriter has already
                        // logged an error if it was a bound attribute; so we can skip.
                        continue;
                    }

                    // We need to capture the tag helper's property value accessor so we can retrieve it later
                    // if there are more tag helpers that need the value.
                    string valueAccessor = null;

                    foreach (var associatedDescriptor in associatedDescriptors)
                    {
                        var associatedAttributeDescriptor = associatedDescriptor.Attributes.First(
                            attributeDescriptor => attributeDescriptor.IsNameMatch(attributeName));
                        var tagHelperVariableName = GetVariableName(associatedDescriptor);

                        valueAccessor = RenderBoundAttribute(
                            attributeName,
                            attributeValueChunk,
                            tagHelperVariableName,
                            valueAccessor,
                            associatedAttributeDescriptor);
                    }
                }
                else
                {
                    RenderUnboundAttribute(attributeName, attributeValueChunk);
                }
            }
        }

        private string RenderBoundAttribute(
            string attributeName,
            Chunk attributeValueChunk,
            string tagHelperVariableName,
            string previousValueAccessor,
            TagHelperAttributeDescriptor attributeDescriptor)
        {
            var currentValueAccessor = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}",
                tagHelperVariableName,
                attributeDescriptor.PropertyName);

            if (attributeDescriptor.IsIndexer)
            {
                var dictionaryKey = attributeName.Substring(attributeDescriptor.Name.Length);
                currentValueAccessor += $"[\"{dictionaryKey}\"]";
            }

            // If this attribute value has not been seen before, need to record its value.
            if (previousValueAccessor == null)
            {
                // Bufferable attributes are attributes that can have Razor code inside of them. Such
                // attributes have string values and may be calculated using a temporary TextWriter or other
                // buffer.
                var bufferableAttribute = attributeDescriptor.IsStringProperty;

                RenderNewAttributeValueAssignment(
                    attributeDescriptor,
                    bufferableAttribute,
                    attributeValueChunk,
                    currentValueAccessor);

                if (_designTimeMode)
                {
                    // Execution contexts are a runtime feature.
                    return currentValueAccessor;
                }

                // We need to inform the context of the attribute value.
                _writer
                    .WriteStartInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        _tagHelperContext.ExecutionContextAddTagHelperAttributeMethodName)
                    .WriteStringLiteral(attributeName)
                    .WriteParameterSeparator()
                    .Write(currentValueAccessor)
                    .WriteEndMethodInvocation();

                return currentValueAccessor;
            }
            else
            {
                // The attribute value has already been determined and accessor was passed to us as
                // previousValueAccessor, we don't want to evaluate the value twice so lets just use the
                // previousValueLocation.
                _writer
                    .WriteStartAssignment(currentValueAccessor)
                    .Write(previousValueAccessor)
                    .WriteLine(";");

                return previousValueAccessor;
            }
        }

        // Render assignment of attribute value to the value accessor.
        private void RenderNewAttributeValueAssignment(
            TagHelperAttributeDescriptor attributeDescriptor,
            bool bufferableAttribute,
            Chunk attributeValueChunk,
            string valueAccessor)
        {
            // Plain text values are non Razor code (@DateTime.Now) values. If an attribute is bufferable it
            // may be more than just a plain text value, it may also contain Razor code which is why we attempt
            // to retrieve a plain text value here.
            string textValue;
            var isPlainTextValue = TryGetPlainTextValue(attributeValueChunk, out textValue);

            if (bufferableAttribute)
            {
                if (!isPlainTextValue)
                {
                    // If we haven't recorded a value and we need to buffer an attribute value and the value is not
                    // plain text then we need to prepare the value prior to setting it below.
                    BuildBufferedWritingScope(attributeValueChunk, htmlEncodeValues: false);
                }

                _writer.WriteStartAssignment(valueAccessor);

                if (isPlainTextValue)
                {
                    // If the attribute is bufferable but has a plain text value that means the value
                    // is a string which needs to be surrounded in quotes.
                    RenderQuotedAttributeValue(textValue, attributeDescriptor);
                }
                else
                {
                    // The value contains more than plain text e.g. stringAttribute ="Time: @DateTime.Now".
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

                    // Write out code expression for this attribute value. Property is not a string.
                    // So quoting or buffering are not helpful.
                    RenderCodeAttributeValue(attributeValueChunk, attributeDescriptor, isPlainTextValue);

                    // End the assignment to the attribute.
                    lineMapper.MarkLineMappingEnd();
                    _writer.WriteLine(";");
                }
            }
        }

        private void RenderUnboundAttribute(string attributeName, Chunk attributeValueChunk)
        {
            // Render children to provide IntelliSense at design time. No need for the execution context logic, it's
            // a runtime feature.
            if (_designTimeMode)
            {
                if (attributeValueChunk != null)
                {
                    _bodyVisitor.Accept(attributeValueChunk);
                }

                return;
            }

            // If we have a minimized attribute there is no value
            if (attributeValueChunk == null)
            {
                _writer
                    .WriteStartInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        _tagHelperContext.ExecutionContextAddMinimizedHtmlAttributeMethodName)
                    .WriteStringLiteral(attributeName)
                    .WriteEndMethodInvocation();
            }
            else
            {
                string textValue = null;
                var isPlainTextValue = TryGetPlainTextValue(attributeValueChunk, out textValue);

                if (isPlainTextValue)
                {
                    // If it's a plain text value then we need to surround the value with quotes.
                    _writer
                        .WriteStartInstanceMethodInvocation(
                            ExecutionContextVariableName,
                            _tagHelperContext.ExecutionContextAddHtmlAttributeMethodName)
                        .WriteStringLiteral(attributeName)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation(_tagHelperContext.MarkAsHtmlEncodedMethodName)
                        .WriteStringLiteral(textValue)
                        .WriteEndMethodInvocation(endLine: false)
                        .WriteEndMethodInvocation();
                }
                else if (IsDynamicAttributeValue(attributeValueChunk))
                {
                    // Dynamic attribute value should be run through the conditional attribute removal system. It's
                    // unbound and contains C#.

                    // TagHelper attribute rendering is buffered by default. We do not want to write to the current
                    // writer.
                    var currentTargetWriter = _context.TargetWriterName;
                    var currentWriteAttributeMethodName = _context.Host.GeneratedClassContext.WriteAttributeValueMethodName;
                    _context.TargetWriterName = null;

                    Debug.Assert(attributeValueChunk is ParentChunk);
                    var children = ((ParentChunk)attributeValueChunk).Children;
                    var attributeCount = children.Count(c => c is DynamicCodeAttributeChunk || c is LiteralCodeAttributeChunk);

                    _writer
                        .WriteStartMethodInvocation(_tagHelperContext.BeginAddHtmlAttributeValuesMethodName)
                        .Write(ExecutionContextVariableName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(attributeName)
                        .WriteParameterSeparator()
                        .Write(attributeCount.ToString(CultureInfo.InvariantCulture))
                        .WriteEndMethodInvocation();

                    _attributeCodeVisitor.Accept(attributeValueChunk);

                    _writer.WriteMethodInvocation(
                        _tagHelperContext.EndAddHtmlAttributeValuesMethodName,
                        ExecutionContextVariableName);

                    _context.TargetWriterName = currentTargetWriter;
                }
                else
                {
                    // HTML attributes are always strings. This attribute contains C# but is not dynamic. This occurs
                    // when the attribute is a data-* attribute.

                    // Attribute value is not plain text, must be buffered to determine its final value.
                    BuildBufferedWritingScope(attributeValueChunk, htmlEncodeValues: true);

                    _writer
                        .WriteStartInstanceMethodInvocation(
                            ExecutionContextVariableName,
                            _tagHelperContext.ExecutionContextAddHtmlAttributeMethodName)
                        .WriteStringLiteral(attributeName)
                        .WriteParameterSeparator()
                        .WriteStartMethodInvocation(_tagHelperContext.MarkAsHtmlEncodedMethodName);

                    RenderBufferedAttributeValueAccessor(_writer);

                    _writer
                        .WriteEndMethodInvocation(endLine: false)
                        .WriteEndMethodInvocation();
                }
            }
        }

        private void RenderEndTagHelpersScope()
        {
            _writer.WriteStartAssignment(ExecutionContextVariableName)
                   .WriteInstanceMethodInvocation(ScopeManagerVariableName,
                                                  _tagHelperContext.ScopeManagerEndMethodName);
        }

        private void RenderWriteTagHelperMethodCall(TagHelperChunk chunk)
        {
            _writer
                .WriteStartInstrumentationContext(_context, chunk.Association, isLiteral: false)
                .Write("await ");

            if (!string.IsNullOrEmpty(_context.TargetWriterName))
            {
                _writer
                    .WriteStartMethodInvocation(_tagHelperContext.WriteTagHelperToAsyncMethodName)
                    .Write(_context.TargetWriterName)
                    .WriteParameterSeparator();
            }
            else
            {
                _writer.WriteStartMethodInvocation(_tagHelperContext.WriteTagHelperAsyncMethodName);
            }

            _writer
                .Write(ExecutionContextVariableName)
                .WriteEndMethodInvocation()
                .WriteEndInstrumentationContext(_context);
        }

        private void RenderRunTagHelpers()
        {
            _writer.Write(ExecutionContextVariableName)
                   .Write(".")
                   .WriteStartAssignment(_tagHelperContext.ExecutionContextOutputPropertyName)
                   .Write("await ")
                   .WriteStartInstanceMethodInvocation(RunnerVariableName,
                                                       _tagHelperContext.RunnerRunAsyncMethodName);

            _writer.Write(ExecutionContextVariableName)
                   .WriteEndMethodInvocation();
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

        private void RenderCodeAttributeValue(
            Chunk attributeValueChunk,
            TagHelperAttributeDescriptor attributeDescriptor,
            bool isPlainTextValue)
        {
            RenderAttributeValue(
                attributeDescriptor,
                valueRenderer: (writer) =>
                {
                    if (attributeDescriptor.IsEnum && isPlainTextValue)
                    {
                        writer.Write(attributeDescriptor.TypeName)
                            .Write(".");
                    }

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

        // Render a buffered writing scope for the HTML attribute value.
        private void BuildBufferedWritingScope(Chunk htmlAttributeChunk, bool htmlEncodeValues)
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

                var visitor = htmlEncodeValues ? _bodyVisitor : _literalBodyVisitor;
                visitor.Accept(htmlAttributeChunk);

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
                writer.WriteInstanceMethodInvocation(
                    StringValueBufferVariableName,
                    _tagHelperContext.TagHelperContentGetContentMethodName,
                    endLine: false,
                    parameters: new string[] { _tagHelperContext.HtmlEncoderPropertyName });
            }
        }

        private static bool IsDynamicAttributeValue(Chunk attributeValueChunk)
        {
            var parentChunk = attributeValueChunk as ParentChunk;
            if (parentChunk != null)
            {
                return parentChunk.Children.Any(child => child is DynamicCodeAttributeChunk);
            }

            return false;
        }

        private static bool TryGetPlainTextValue(Chunk chunk, out string plainText)
        {
            var parentChunk = chunk as ParentChunk;

            plainText = null;

            if (parentChunk == null || parentChunk.Children.Count != 1)
            {
                return false;
            }

            var literalChildChunk = parentChunk.Children[0] as LiteralChunk;

            if (literalChildChunk == null)
            {
                return false;
            }

            plainText = literalChildChunk.Text;

            return true;
        }

        // A CSharpCodeVisitor which does not HTML encode values. Used when rendering bound string attribute values.
        private class CSharpLiteralCodeVisitor : CSharpCodeVisitor
        {
            public CSharpLiteralCodeVisitor(
                CSharpTagHelperCodeRenderer tagHelperRenderer,
                CSharpCodeWriter writer,
                CodeGeneratorContext context)
                : base(writer, context)
            {
                // Ensure that no matter how this class is used, we don't create numerous CSharpTagHelperCodeRenderer
                // instances.
                TagHelperRenderer = tagHelperRenderer;
            }

            protected override string WriteMethodName
            {
                get
                {
                    return Context.Host.GeneratedClassContext.WriteLiteralMethodName;
                }
            }

            protected override string WriteToMethodName
            {
                get
                {
                    return Context.Host.GeneratedClassContext.WriteLiteralToMethodName;
                }
            }
        }

        private class TagHelperAttributeCodeVisitor : CSharpCodeVisitor
        {
            public TagHelperAttributeCodeVisitor(
                CSharpCodeWriter writer,
                CodeGeneratorContext context)
                : base(writer, context)
            {
            }

            protected override string WriteAttributeValueMethodName =>
                Context.Host.GeneratedClassContext.GeneratedTagHelperContext.AddHtmlAttributeValueMethodName;
        }
    }
}