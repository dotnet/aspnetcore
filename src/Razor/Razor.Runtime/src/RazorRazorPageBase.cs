// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor
{
    /// <summary>
    /// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    public abstract class RazorRazorPageBase<TOutputContext> : IRazorRazorPage<TOutputContext> where TOutputContext : IOutputContext
    {
        private readonly Stack<TextWriter> _textWriterStack = new Stack<TextWriter>();
        private StringWriter _valueBuffer;
        private AttributeInfo _attributeInfo;
        private TagHelperAttributeInfo _tagHelperAttributeInfo;

        public virtual TOutputContext ViewContext { get; set; }

        public string Layout { get; set; }

        /// <summary>
        /// Gets the <see cref="TextWriter"/> that the page is writing output to.
        /// </summary>
        /// <summary>
        /// Gets the <see cref="TextWriter"/> that the page is writing output to.
        /// </summary>
        public virtual TextWriter Output
        {
            get
            {
                var viewContext = ViewContext;
                if (viewContext == null)
                {
                    throw new InvalidOperationException("View context must be set"); //TODO Resources.FormatViewContextMustBeSet(nameof(ViewContext), nameof(Output)));
                }

                return viewContext.Writer;
            }
        }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; } =
            new Dictionary<string, RenderAsyncDelegate>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the dynamic view data dictionary.
        /// </summary>
        public dynamic ViewBag => ViewContext?.ViewBag;

        /// <inheritdoc />
        public bool IsLayoutBeingRendered { get; set; }

        /// <inheritdoc />
        public IHtmlContent BodyContent { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorRazorPageBase{TOutputContext}"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        public virtual HtmlEncoder HtmlEncoder { get; set; }

        public abstract Task ExecuteAsync();

        // Internal for unit testing.
        protected internal virtual void PushWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var viewContext = ViewContext;
            _textWriterStack.Push(viewContext.Writer);
            viewContext.Writer = writer;
        }

        // Internal for unit testing.
        protected internal virtual TextWriter PopWriter()
        {
            var viewContext = ViewContext;
            var writer = _textWriterStack.Pop();
            viewContext.Writer = writer;
            return writer;
        }

        /// <inheritdoc />
        public abstract string Href(string contentPath);

        /// <summary>
        /// Creates a named content section in the page that can be invoked in a Layout page using
        /// <c>RenderSection</c> or <c>RenderSectionAsync</c>
        /// </summary>
        /// <param name="name">The name of the section to create.</param>
        /// <param name="section">The delegate to execute when rendering the section.</param>
        /// <remarks>This is a temporary placeholder method to support ASP.NET Core 2.0.0 editor code generation.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected void DefineSection(string name, Func<object, Task> section)
            => DefineSection(name, () => section(null /* writer */));

        /// <summary>
        /// Creates a named content section in the page that can be invoked in a Layout page using
        /// <c>RenderSection</c> or <c>RenderSectionAsync</c>
        /// </summary>
        /// <param name="name">The name of the section to create.</param>
        /// <param name="section">The <see cref="RenderAsyncDelegate"/> to execute when rendering the section.</param>
        public virtual void DefineSection(string name, RenderAsyncDelegate section)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            if (SectionWriters.ContainsKey(name))
            {
                // TODO throw new InvalidOperationException(Resources.FormatSectionAlreadyDefined(name));
                throw new InvalidOperationException($"Format Section Already Defined ({name})");
            }
            SectionWriters[name] = section;
        }


        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to write.</param>
        public virtual void Write(object value)
        {
            if (value == null || value == HtmlString.Empty)
            {
                return;
            }

            var writer = Output;
            var encoder = HtmlEncoder;
            if (value is IHtmlContent htmlContent)
            {
                // TODO boffered writing in impl
                htmlContent.WriteTo(writer, encoder);
                return;
            }

            Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to write.</param>
        public virtual void Write(string value)
        {
            var writer = Output;
            var encoder = HtmlEncoder;
            if (!string.IsNullOrEmpty(value))
            {
                // Perf: Encode right away instead of writing it character-by-character.
                // character-by-character isn't efficient when using a writer backed by a ViewBuffer.
                var encoded = encoder.Encode(value);
                writer.Write(encoded);
            }
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to write.</param>
        public virtual void WriteLiteral(object value)
        {
            if (value == null)
            {
                return;
            }

            WriteLiteral(value.ToString());
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to write.</param>
        public virtual void WriteLiteral(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Output.Write(value);
            }
        }

        public virtual void BeginWriteAttribute(
            string name,
            string prefix,
            int prefixOffset,
            string suffix,
            int suffixOffset,
            int attributeValuesCount)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            _attributeInfo = new AttributeInfo(name, prefix, prefixOffset, suffix, suffixOffset, attributeValuesCount);

            // Single valued attributes might be omitted in entirety if it the attribute value strictly evaluates to
            // null  or false. Consequently defer the prefix generation until we encounter the attribute value.
            if (attributeValuesCount != 1)
            {
                WritePositionTaggedLiteral(prefix, prefixOffset);
            }
        }

        public void WriteAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
            if (_attributeInfo.AttributeValuesCount == 1)
            {
                if (IsBoolFalseOrNullValue(prefix, value))
                {
                    // Value is either null or the bool 'false' with no prefix; don't render the attribute.
                    _attributeInfo.Suppressed = true;
                    return;
                }

                // We are not omitting the attribute. Write the prefix.
                WritePositionTaggedLiteral(_attributeInfo.Prefix, _attributeInfo.PrefixOffset);

                if (IsBoolTrueWithEmptyPrefixValue(prefix, value))
                {
                    // The value is just the bool 'true', write the attribute name instead of the string 'True'.
                    value = _attributeInfo.Name;
                }
            }

            // This block handles two cases.
            // 1. Single value with prefix.
            // 2. Multiple values with or without prefix.
            if (value != null)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    WritePositionTaggedLiteral(prefix, prefixOffset);
                }

                BeginContext(valueOffset, valueLength, isLiteral);

                WriteUnprefixedAttributeValue(value, isLiteral);

                EndContext();
            }
        }

        public virtual void EndWriteAttribute()
        {
            if (!_attributeInfo.Suppressed)
            {
                WritePositionTaggedLiteral(_attributeInfo.Suffix, _attributeInfo.SuffixOffset);
            }
        }

        public void BeginAddHtmlAttributeValues(
            TagHelperExecutionContext executionContext,
            string attributeName,
            int attributeValuesCount,
            HtmlAttributeValueStyle attributeValueStyle)
        {
            _tagHelperAttributeInfo = new TagHelperAttributeInfo(
                executionContext,
                attributeName,
                attributeValuesCount,
                attributeValueStyle);
        }

        public void AddHtmlAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
            Debug.Assert(_tagHelperAttributeInfo.ExecutionContext != null);
            if (_tagHelperAttributeInfo.AttributeValuesCount == 1)
            {
                if (IsBoolFalseOrNullValue(prefix, value))
                {
                    // The first value was 'null' or 'false' indicating that we shouldn't render the attribute. The
                    // attribute is treated as a TagHelper attribute so it's only available in
                    // TagHelperContext.AllAttributes for TagHelper authors to see (if they want to see why the
                    // attribute was removed from TagHelperOutput.Attributes).
                    _tagHelperAttributeInfo.ExecutionContext.AddTagHelperAttribute(
                        _tagHelperAttributeInfo.Name,
                        value?.ToString() ?? string.Empty,
                        _tagHelperAttributeInfo.AttributeValueStyle);
                    _tagHelperAttributeInfo.Suppressed = true;
                    return;
                }
                else if (IsBoolTrueWithEmptyPrefixValue(prefix, value))
                {
                    _tagHelperAttributeInfo.ExecutionContext.AddHtmlAttribute(
                        _tagHelperAttributeInfo.Name,
                        _tagHelperAttributeInfo.Name,
                        _tagHelperAttributeInfo.AttributeValueStyle);
                    _tagHelperAttributeInfo.Suppressed = true;
                    return;
                }
            }

            if (value != null)
            {
                // Perf: We'll use this buffer for all of the attribute values and then clear it to
                // reduce allocations.
                if (_valueBuffer == null)
                {
                    _valueBuffer = new StringWriter();
                }

                PushWriter(_valueBuffer);
                if (!string.IsNullOrEmpty(prefix))
                {
                    WriteLiteral(prefix);
                }

                WriteUnprefixedAttributeValue(value, isLiteral);
                PopWriter();
            }
        }

        public void EndAddHtmlAttributeValues(TagHelperExecutionContext executionContext)
        {
            if (!_tagHelperAttributeInfo.Suppressed)
            {
                // Perf: _valueBuffer might be null if nothing was written. If it is set, clear it so
                // it is reset for the next value.
                var content = _valueBuffer == null ? HtmlString.Empty : new HtmlString(_valueBuffer.ToString());
                _valueBuffer?.GetStringBuilder().Clear();

                executionContext.AddHtmlAttribute(_tagHelperAttributeInfo.Name, content, _tagHelperAttributeInfo.AttributeValueStyle);
            }
        }

        /// <summary>
        /// Invokes <see cref="IOutputContext.FlushAsync"/>.
        /// </summary>
        /// <returns>A <see cref="Task{HtmlString}"/> that represents the asynchronous flush operation and on
        /// completion returns an empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks>The value returned is a token value that allows FlushAsync to work directly in an HTML
        /// section. However the value does not represent the rendered content.
        /// </remarks>
        public virtual async Task<IHtmlContent> FlushAsync()
        {
            // Calls to Flush are allowed if the page does not specify a Layout or if it is executing a section in the
            // Layout.
            if (!IsLayoutBeingRendered && !string.IsNullOrEmpty(Layout))
            {
                // TODO var message = Resources.FormatLayoutCannotBeRendered(Path, nameof(FlushAsync));
                var message = $"Format Layout Cannot Be Rendered ({Path}, {nameof(FlushAsync)})";
                throw new InvalidOperationException(message);
            }
            await ViewContext.FlushAsync();
            return HtmlString.Empty;
        }

        protected abstract Task OnFlushAsync();

        private void WriteUnprefixedAttributeValue(object value, bool isLiteral)
        {
            var stringValue = value as string;

            // The extra branching here is to ensure that we call the Write*To(string) overload where possible.
            if (isLiteral && stringValue != null)
            {
                WriteLiteral(stringValue);
            }
            else if (isLiteral)
            {
                WriteLiteral(value);
            }
            else if (stringValue != null)
            {
                Write(stringValue);
            }
            else
            {
                Write(value);
            }
        }

        private void WritePositionTaggedLiteral(string value, int position)
        {
            BeginContext(position, value.Length, isLiteral: true);
            WriteLiteral(value);
            EndContext();
        }

        public abstract void BeginContext(int position, int length, bool isLiteral);

        public abstract void EndContext();

        private bool IsBoolFalseOrNullValue(string prefix, object value)
        {
            return string.IsNullOrEmpty(prefix) &&
                (value == null ||
                (value is bool && !(bool)value));
        }

        private bool IsBoolTrueWithEmptyPrefixValue(string prefix, object value)
        {
            // If the value is just the bool 'true', use the attribute name as the value.
            return string.IsNullOrEmpty(prefix) &&
                (value is bool && (bool)value);
        }

        public abstract void EnsureRenderedBodyOrSections();

        private struct AttributeInfo
        {
            public AttributeInfo(
                string name,
                string prefix,
                int prefixOffset,
                string suffix,
                int suffixOffset,
                int attributeValuesCount)
            {
                Name = name;
                Prefix = prefix;
                PrefixOffset = prefixOffset;
                Suffix = suffix;
                SuffixOffset = suffixOffset;
                AttributeValuesCount = attributeValuesCount;

                Suppressed = false;
            }

            public int AttributeValuesCount { get; }

            public string Name { get; }

            public string Prefix { get; }

            public int PrefixOffset { get; }

            public string Suffix { get; }

            public int SuffixOffset { get; }

            public bool Suppressed { get; set; }
        }

        private struct TagHelperAttributeInfo
        {
            public TagHelperAttributeInfo(
                TagHelperExecutionContext tagHelperExecutionContext,
                string name,
                int attributeValuesCount,
                HtmlAttributeValueStyle attributeValueStyle)
            {
                ExecutionContext = tagHelperExecutionContext;
                Name = name;
                AttributeValuesCount = attributeValuesCount;
                AttributeValueStyle = attributeValueStyle;

                Suppressed = false;
            }

            public string Name { get; }

            public TagHelperExecutionContext ExecutionContext { get; }

            public int AttributeValuesCount { get; }

            public HtmlAttributeValueStyle AttributeValueStyle { get; }

            public bool Suppressed { get; set; }
        }
    }
}