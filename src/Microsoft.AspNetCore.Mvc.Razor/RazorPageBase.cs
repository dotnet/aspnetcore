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
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    public abstract class RazorPageBase : IRazorPage
    {
        private StringWriter _valueBuffer;
        private ITagHelperFactory _tagHelperFactory;
        private IViewBufferScope _bufferScope;
        private TextWriter _pageWriter;
        private AttributeInfo _attributeInfo;
        private TagHelperAttributeInfo _tagHelperAttributeInfo;
        private IUrlHelper _urlHelper;

        public ViewContext ViewContext { get; set; }

        public string Layout { get; set; }

        /// <summary>
        /// An <see cref="HttpContext"/> representing the current request execution.
        /// </summary>
        public HttpContext Context => ViewContext?.HttpContext;

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
                if (ViewContext == null)
                {
                    var message = Resources.FormatViewContextMustBeSet("ViewContext", "Output");
                    throw new InvalidOperationException(message);
                }

                return ViewContext.Writer;
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
        /// Gets or sets a <see cref="System.Diagnostics.DiagnosticSource"/> instance used to instrument the page execution.
        /// </summary>
        [RazorInject]
        public DiagnosticSource DiagnosticSource { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        [RazorInject]
        public HtmlEncoder HtmlEncoder { get; set; }

        /// <summary>
        /// Gets the <see cref="ClaimsPrincipal"/> of the current logged in user.
        /// </summary>
        public virtual ClaimsPrincipal User => Context?.User;

        protected Stack<TagHelperScopeInfo> TagHelperScopes { get; } = new Stack<TagHelperScopeInfo>();

        private ITagHelperFactory TagHelperFactory
        {
            get
            {
                if (_tagHelperFactory == null)
                {
                    var services = ViewContext.HttpContext.RequestServices;
                    _tagHelperFactory = services.GetRequiredService<ITagHelperFactory>();
                }

                return _tagHelperFactory;
            }
        }

        private IViewBufferScope BufferScope
        {
            get
            {
                if (_bufferScope == null)
                {
                    var services = ViewContext.HttpContext.RequestServices;
                    _bufferScope = services.GetRequiredService<IViewBufferScope>();
                }

                return _bufferScope;
            }
        }

        public abstract Task ExecuteAsync();

        /// <summary>
        /// Format an error message about using an indexer when the tag helper property is <c>null</c>.
        /// </summary>
        /// <param name="attributeName">Name of the HTML attribute associated with the indexer.</param>
        /// <param name="tagHelperTypeName">Full name of the tag helper <see cref="Type"/>.</param>
        /// <param name="propertyName">Dictionary property in the tag helper.</param>
        /// <returns>An error message about using an indexer when the tag helper property is <c>null</c>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string InvalidTagHelperIndexerAssignment(
            string attributeName,
            string tagHelperTypeName,
            string propertyName)
        {
            return Resources.FormatRazorPage_InvalidTagHelperIndexerAssignment(
                attributeName,
                tagHelperTypeName,
                propertyName);
        }

        /// <summary>
        /// Creates and activates a <see cref="ITagHelper"/>.
        /// </summary>
        /// <typeparam name="TTagHelper">A <see cref="ITagHelper"/> type.</typeparam>
        /// <returns>The activated <see cref="ITagHelper"/>.</returns>
        /// <remarks>
        /// <typeparamref name="TTagHelper"/> must have a parameterless constructor.
        /// </remarks>
        public TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper
        {
            return TagHelperFactory.CreateTagHelper<TTagHelper>(ViewContext);
        }

        /// <summary>
        /// Starts a new writing scope and optionally overrides <see cref="HtmlEncoder"/> within that scope.
        /// </summary>
        /// <param name="encoder">
        /// The <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/> handles
        /// non-<see cref="IHtmlContent"/> C# expressions. If <c>null</c>, does not change <see cref="HtmlEncoder"/>.
        /// </param>
        /// <remarks>
        /// All writes to the <see cref="Output"/> or <see cref="ViewContext.Writer"/> after calling this method will
        /// be buffered until <see cref="EndTagHelperWritingScope"/> is called.
        /// </remarks>
        public void StartTagHelperWritingScope(HtmlEncoder encoder)
        {
            var buffer = new ViewBuffer(BufferScope, Path, ViewBuffer.TagHelperPageSize);
            TagHelperScopes.Push(new TagHelperScopeInfo(buffer, HtmlEncoder, ViewContext.Writer));

            // If passed an HtmlEncoder, override the property.
            if (encoder != null)
            {
                HtmlEncoder = encoder;
            }

            // We need to replace the ViewContext's Writer to ensure that all content (including content written
            // from HTML helpers) is redirected.
            ViewContext.Writer = new ViewBufferTextWriter(buffer, ViewContext.Writer.Encoding);
        }

        /// <summary>
        /// Ends the current writing scope that was started by calling <see cref="StartTagHelperWritingScope"/>.
        /// </summary>
        /// <returns>The buffered <see cref="TagHelperContent"/>.</returns>
        public TagHelperContent EndTagHelperWritingScope()
        {
            if (TagHelperScopes.Count == 0)
            {
                throw new InvalidOperationException(Resources.RazorPage_ThereIsNoActiveWritingScopeToEnd);
            }

            var scopeInfo = TagHelperScopes.Pop();

            // Get the content written during the current scope.
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.AppendHtml(scopeInfo.Buffer);

            // Restore previous scope.
            HtmlEncoder = scopeInfo.HtmlEncoder;
            ViewContext.Writer = scopeInfo.Writer;

            return tagHelperContent;
        }

        /// <summary>
        /// Starts a new scope for writing <see cref="ITagHelper"/> attribute values.
        /// </summary>
        /// <remarks>
        /// All writes to the <see cref="Output"/> or <see cref="ViewContext.Writer"/> after calling this method will
        /// be buffered until <see cref="EndWriteTagHelperAttribute"/> is called.
        /// The content will be buffered using a shared <see cref="StringWriter"/> within this <see cref="RazorPage"/>
        /// Nesting of <see cref="BeginWriteTagHelperAttribute"/> and <see cref="EndWriteTagHelperAttribute"/> method calls
        /// is not supported.
        /// </remarks>
        public void BeginWriteTagHelperAttribute()
        {
            if (_pageWriter != null)
            {
                throw new InvalidOperationException(Resources.RazorPage_NestingAttributeWritingScopesNotSupported);
            }

            _pageWriter = ViewContext.Writer;

            if (_valueBuffer == null)
            {
                _valueBuffer = new StringWriter();
            }

            // We need to replace the ViewContext's Writer to ensure that all content (including content written
            // from HTML helpers) is redirected.
            ViewContext.Writer = _valueBuffer;

        }

        /// <summary>
        /// Ends the current writing scope that was started by calling <see cref="BeginWriteTagHelperAttribute"/>.
        /// </summary>
        /// <returns>The content buffered by the shared <see cref="StringWriter"/> of this <see cref="RazorPage"/>.</returns>
        /// <remarks>
        /// This method assumes that there will be no nesting of <see cref="BeginWriteTagHelperAttribute"/>
        /// and <see cref="EndWriteTagHelperAttribute"/> method calls.
        /// </remarks>
        public string EndWriteTagHelperAttribute()
        {
            if (_pageWriter == null)
            {
                throw new InvalidOperationException(Resources.RazorPage_ThereIsNoActiveWritingScopeToEnd);
            }

            var content = _valueBuffer.ToString();
            _valueBuffer.GetStringBuilder().Clear();

            // Restore previous writer.
            ViewContext.Writer = _pageWriter;
            _pageWriter = null;

            return content;
        }

        public virtual string Href(string contentPath)
        {
            if (contentPath == null)
            {
                throw new ArgumentNullException(nameof(contentPath));
            }

            if (_urlHelper == null)
            {
                var services = Context.RequestServices;
                var factory = services.GetRequiredService<IUrlHelperFactory>();
                _urlHelper = factory.GetUrlHelper(ViewContext);
            }

            return _urlHelper.Content(contentPath);
        }

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
                throw new InvalidOperationException(Resources.FormatSectionAlreadyDefined(name));
            }
            SectionWriters[name] = section;
        }


        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to write.</param>
        public virtual void Write(object value)
        {
            WriteTo(Output, value);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <remarks>
        /// <paramref name="value"/>s of type <see cref="IHtmlContent"/> are written using
        /// <see cref="IHtmlContent.WriteTo(TextWriter, HtmlEncoder)"/>.
        /// For all other types, the encoded result of <see cref="object.ToString"/> is written to the
        /// <paramref name="writer"/>.
        /// </remarks>
        public virtual void WriteTo(TextWriter writer, object value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteTo(writer, HtmlEncoder, value);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to given <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="encoder">
        /// The <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when encoding <paramref name="value"/>.
        /// </param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <remarks>
        /// <paramref name="value"/>s of type <see cref="IHtmlContent"/> are written using
        /// <see cref="IHtmlContent.WriteTo(TextWriter, HtmlEncoder)"/>.
        /// For all other types, the encoded result of <see cref="object.ToString"/> is written to the
        /// <paramref name="writer"/>.
        /// </remarks>
        public static void WriteTo(TextWriter writer, HtmlEncoder encoder, object value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (value == null || value == HtmlString.Empty)
            {
                return;
            }

            var htmlContent = value as IHtmlContent;
            if (htmlContent != null)
            {
                var bufferedWriter = writer as ViewBufferTextWriter;
                if (bufferedWriter == null || !bufferedWriter.IsBuffering)
                {
                    htmlContent.WriteTo(writer, encoder);
                }
                else
                {
                    var htmlContentContainer = value as IHtmlContentContainer;
                    if (htmlContentContainer != null)
                    {
                        // This is likely another ViewBuffer.
                        htmlContentContainer.MoveTo(bufferedWriter.Buffer);
                    }
                    else
                    {
                        // Perf: This is the common case for IHtmlContent, ViewBufferTextWriter is inefficient
                        // for writing character by character.
                        bufferedWriter.Buffer.AppendHtml(htmlContent);
                    }
                }

                return;
            }

            WriteTo(writer, encoder, value.ToString());
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="string"/> to write.</param>
        public virtual void WriteTo(TextWriter writer, string value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteTo(writer, HtmlEncoder, value);
        }

        private static void WriteTo(TextWriter writer, HtmlEncoder encoder, string value)
        {
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
            WriteLiteralTo(Output, value);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        public virtual void WriteLiteralTo(TextWriter writer, object value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (value != null)
            {
                WriteLiteralTo(writer, value.ToString());
            }
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="string"/> to write.</param>
        public virtual void WriteLiteralTo(TextWriter writer, string value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (!string.IsNullOrEmpty(value))
            {
                writer.Write(value);
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
            BeginWriteAttributeTo(Output, name, prefix, prefixOffset, suffix, suffixOffset, attributeValuesCount);
        }

        public virtual void BeginWriteAttributeTo(
            TextWriter writer,
            string name,
            string prefix,
            int prefixOffset,
            string suffix,
            int suffixOffset,
            int attributeValuesCount)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

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
                WritePositionTaggedLiteral(writer, prefix, prefixOffset);
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
            WriteAttributeValueTo(Output, prefix, prefixOffset, value, valueOffset, valueLength, isLiteral);
        }

        public void WriteAttributeValueTo(
            TextWriter writer,
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
                WritePositionTaggedLiteral(writer, _attributeInfo.Prefix, _attributeInfo.PrefixOffset);

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
                    WritePositionTaggedLiteral(writer, prefix, prefixOffset);
                }

                BeginContext(valueOffset, valueLength, isLiteral);

                WriteUnprefixedAttributeValueTo(writer, value, isLiteral);

                EndContext();
            }
        }

        public virtual void EndWriteAttribute()
        {
            EndWriteAttributeTo(Output);
        }

        public virtual void EndWriteAttributeTo(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (!_attributeInfo.Suppressed)
            {
                WritePositionTaggedLiteral(writer, _attributeInfo.Suffix, _attributeInfo.SuffixOffset);
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

                if (!string.IsNullOrEmpty(prefix))
                {
                    WriteLiteralTo(_valueBuffer, prefix);
                }

                WriteUnprefixedAttributeValueTo(_valueBuffer, value, isLiteral);
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
        /// Invokes <see cref="TextWriter.FlushAsync"/> on <see cref="Output"/> and <see cref="m:Stream.FlushAsync"/>
        /// on the response stream, writing out any buffered content to the <see cref="HttpResponse.Body"/>.
        /// </summary>
        /// <returns>A <see cref="Task{HtmlString}"/> that represents the asynchronous flush operation and on
        /// completion returns an empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks>The value returned is a token value that allows FlushAsync to work directly in an HTML
        /// section. However the value does not represent the rendered content.
        /// This method also writes out headers, so any modifications to headers must be done before
        /// <see cref="FlushAsync"/> is called. For example, call <see cref="SetAntiforgeryCookieAndHeader"/> to send
        /// antiforgery cookie token and X-Frame-Options header to client before this method flushes headers out.
        /// </remarks>
        public virtual async Task<HtmlString> FlushAsync()
        {
            // If there are active scopes, then we should throw. Cannot flush content that has the potential to change.
            if (TagHelperScopes.Count > 0)
            {
                throw new InvalidOperationException(
                    Resources.FormatRazorPage_CannotFlushWhileInAWritingScope(nameof(FlushAsync), Path));
            }

            // Calls to Flush are allowed if the page does not specify a Layout or if it is executing a section in the
            // Layout.
            if (!IsLayoutBeingRendered && !string.IsNullOrEmpty(Layout))
            {
                var message = Resources.FormatLayoutCannotBeRendered(Path, nameof(FlushAsync));
                throw new InvalidOperationException(message);
            }

            await Output.FlushAsync();
            await Context.Response.Body.FlushAsync();
            return HtmlString.Empty;
        }

        /// <summary>
        /// Sets antiforgery cookie and X-Frame-Options header on the response.
        /// </summary>
        /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks> Call this method to send antiforgery cookie token and X-Frame-Options header to client
        /// before <see cref="RazorPageBase.FlushAsync"/> flushes the headers. </remarks>
        public virtual HtmlString SetAntiforgeryCookieAndHeader()
        {
            var antiforgery = Context.RequestServices.GetRequiredService<IAntiforgery>();
            antiforgery.SetCookieTokenAndHeader(Context);

            return HtmlString.Empty;
        }

        private void WriteUnprefixedAttributeValueTo(TextWriter writer, object value, bool isLiteral)
        {
            var stringValue = value as string;

            // The extra branching here is to ensure that we call the Write*To(string) overload where possible.
            if (isLiteral && stringValue != null)
            {
                WriteLiteralTo(writer, stringValue);
            }
            else if (isLiteral)
            {
                WriteLiteralTo(writer, value);
            }
            else if (stringValue != null)
            {
                WriteTo(writer, stringValue);
            }
            else
            {
                WriteTo(writer, value);
            }
        }

        private void WritePositionTaggedLiteral(TextWriter writer, string value, int position)
        {
            BeginContext(position, value.Length, isLiteral: true);
            WriteLiteralTo(writer, value);
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

        protected struct TagHelperScopeInfo
        {
            public TagHelperScopeInfo(ViewBuffer buffer, HtmlEncoder encoder, TextWriter writer)
            {
                Buffer = buffer;
                HtmlEncoder = encoder;
                Writer = writer;
            }

            public ViewBuffer Buffer { get; }

            public HtmlEncoder HtmlEncoder { get; }

            public TextWriter Writer { get; }
        }
    }
}