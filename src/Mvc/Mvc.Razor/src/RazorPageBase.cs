// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
/// </summary>
public abstract class RazorPageBase : IRazorPage
{
    private readonly Stack<TextWriter> _textWriterStack = new Stack<TextWriter>();
    private StringWriter? _valueBuffer;
    private ITagHelperFactory? _tagHelperFactory;
    private IViewBufferScope? _bufferScope;
    private TextWriter? _pageWriter;
    private AttributeInfo _attributeInfo;
    private TagHelperAttributeInfo _tagHelperAttributeInfo;
    private IUrlHelper? _urlHelper;

    /// <inheritdoc/>
    public virtual ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc/>
    public string? Layout { get; set; }

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
                throw new InvalidOperationException(Resources.FormatViewContextMustBeSet(nameof(ViewContext), nameof(Output)));
            }

            return viewContext.Writer;
        }
    }

    /// <inheritdoc />
    public string Path { get; set; } = default!;

    /// <inheritdoc />
    public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; } =
        new Dictionary<string, RenderAsyncDelegate>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the dynamic view data dictionary.
    /// </summary>
    public dynamic ViewBag => ViewContext?.ViewBag!;

    /// <inheritdoc />
    public bool IsLayoutBeingRendered { get; set; }

    /// <inheritdoc />
    public IHtmlContent? BodyContent { get; set; }

    /// <inheritdoc />
    public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; } = default!;

    /// <summary>
    /// Gets or sets a <see cref="System.Diagnostics.DiagnosticSource"/> instance used to instrument the page execution.
    /// </summary>
    [RazorInject]
    public DiagnosticSource DiagnosticSource { get; set; } = default!;

    /// <summary>
    /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
    /// handles non-<see cref="IHtmlContent"/> C# expressions.
    /// </summary>
    [RazorInject]
    public HtmlEncoder HtmlEncoder { get; set; } = default!;

    /// <summary>
    /// Gets the <see cref="ClaimsPrincipal"/> of the current logged in user.
    /// </summary>
    public virtual ClaimsPrincipal User => ViewContext.HttpContext.User;

    /// <summary>
    /// Gets the <see cref="ITempDataDictionary"/> from the <see cref="ViewContext"/>.
    /// </summary>
    /// <remarks>Returns null if <see cref="ViewContext"/> is null.</remarks>
    public ITempDataDictionary TempData => ViewContext?.TempData!;

    private Stack<TagHelperScopeInfo> TagHelperScopes { get; } = new Stack<TagHelperScopeInfo>();

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

    /// <inheritdoc/>
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
        var viewContext = ViewContext;
        var buffer = new ViewBuffer(BufferScope, Path, ViewBuffer.TagHelperPageSize);
        TagHelperScopes.Push(new TagHelperScopeInfo(buffer, HtmlEncoder, viewContext.Writer));

        // If passed an HtmlEncoder, override the property.
        if (encoder != null)
        {
            HtmlEncoder = encoder;
        }

        // We need to replace the ViewContext's Writer to ensure that all content (including content written
        // from HTML helpers) is redirected.
        viewContext.Writer = new ViewBufferTextWriter(buffer, viewContext.Writer.Encoding);
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

        var viewContext = ViewContext;
        _pageWriter = viewContext.Writer;

        if (_valueBuffer == null)
        {
            _valueBuffer = new StringWriter();
        }

        // We need to replace the ViewContext's Writer to ensure that all content (including content written
        // from HTML helpers) is redirected.
        viewContext.Writer = _valueBuffer;
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

        Debug.Assert(_valueBuffer is not null);

        var content = _valueBuffer.ToString();
        _valueBuffer.GetStringBuilder().Clear();

        // Restore previous writer.
        ViewContext.Writer = _pageWriter;
        _pageWriter = null;

        return content;
    }

    /// <summary>
    /// Puts a text writer on the stack.
    /// </summary>
    /// <param name="writer"></param>
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

    /// <summary>
    /// Return a text writer from the stack.
    /// </summary>
    /// <returns>The text writer.</returns>
    // Internal for unit testing.
    protected internal virtual TextWriter PopWriter()
    {
        var viewContext = ViewContext;
        var writer = _textWriterStack.Pop();
        viewContext.Writer = writer;
        return writer;
    }

    /// <summary>
    /// Returns a href for the given content path.
    /// </summary>
    /// <param name="contentPath">The content path.</param>
    /// <returns>The href for the contentPath.</returns>
    public virtual string Href(string contentPath)
    {
        if (contentPath == null)
        {
            throw new ArgumentNullException(nameof(contentPath));
        }

        if (_urlHelper == null)
        {
            var viewContext = ViewContext;
            var services = viewContext.HttpContext.RequestServices;
            var factory = services.GetRequiredService<IUrlHelperFactory>();
            _urlHelper = factory.GetUrlHelper(viewContext);
        }

        return _urlHelper.Content(contentPath);
    }

    /// <summary>
    /// Creates a named content section in the page that can be invoked in a Layout page using
    /// <c>RenderSection</c> or <c>RenderSectionAsync</c>
    /// </summary>
    /// <param name="name">The name of the section to create.</param>
    /// <param name="section">The delegate to execute when rendering the section.</param>
    /// <remarks>This is a temporary placeholder method to support ASP.NET Core 2.0.0 editor code generation.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected void DefineSection(string name, Func<object?, Task> section)
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
            throw new InvalidOperationException(Resources.FormatSectionAlreadyDefined(name));
        }
        SectionWriters[name] = section;
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to write.</param>
    public virtual void Write(object? value)
    {
        if (value is null || value == HtmlString.Empty)
        {
            return;
        }

        var writer = Output;
        var encoder = HtmlEncoder;
        if (value is IHtmlContent htmlContent)
        {
            if (writer is ViewBufferTextWriter bufferedWriter)
            {
                if (value is IHtmlContentContainer htmlContentContainer)
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
            else
            {
                htmlContent.WriteTo(writer, encoder);
            }

            return;
        }

        Write(value.ToString());
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public virtual void Write(string? value)
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
    public virtual void WriteLiteral(object? value)
    {
        if (value is null)
        {
            return;
        }

        WriteLiteral(value.ToString());
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to write.</param>
    public virtual void WriteLiteral(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Output.Write(value);
        }
    }

    /// <summary>
    /// Begins writing out an attribute.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="prefixOffset">The prefix offset.</param>
    /// <param name="suffix">The suffix.</param>
    /// <param name="suffixOffset">The suffix offset.</param>
    /// <param name="attributeValuesCount">The attribute values count.</param>
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

    /// <summary>
    /// Writes out an attribute value.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="prefixOffset">The prefix offset.</param>
    /// <param name="value">The value.</param>
    /// <param name="valueOffset">The value offset.</param>
    /// <param name="valueLength">The value length.</param>
    /// <param name="isLiteral">Whether the attribute is a literal.</param>
    public void WriteAttributeValue(
        string prefix,
        int prefixOffset,
        object? value,
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

    /// <summary>
    /// Ends writing an attribute.
    /// </summary>
    public virtual void EndWriteAttribute()
    {
        if (!_attributeInfo.Suppressed)
        {
            WritePositionTaggedLiteral(_attributeInfo.Suffix, _attributeInfo.SuffixOffset);
        }
    }

    /// <summary>
    /// Begins adding html attribute values.
    /// </summary>
    /// <param name="executionContext">The <see cref="TagHelperExecutionContext"/>.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="attributeValuesCount">The number of attribute values.</param>
    /// <param name="attributeValueStyle">The <see cref="HtmlAttributeValueStyle"/>.</param>
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

    /// <summary>
    /// Add an html attribute value.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="prefixOffset">The prefix offset.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="valueOffset">The value offset.</param>
    /// <param name="valueLength">The value length.</param>
    /// <param name="isLiteral">Whether the attribute is a literal.</param>
    public void AddHtmlAttributeValue(
        string? prefix,
        int prefixOffset,
        object? value,
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

    /// <summary>
    /// Ends adding html attribute values.
    /// </summary>
    /// <param name="executionContext">The <see cref="TagHelperExecutionContext"/>.</param>
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
        await ViewContext.HttpContext.Response.Body.FlushAsync();
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
        var viewContext = ViewContext;
        if (viewContext != null)
        {
            var antiforgery = viewContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            antiforgery.SetCookieTokenAndHeader(viewContext.HttpContext);
        }
        return HtmlString.Empty;
    }

    private void WriteUnprefixedAttributeValue(object? value, bool isLiteral)
    {
        // The extra branching here is to ensure that we call the Write*To(string) overload where possible.
        if (value is string stringValue)
        {
            if (isLiteral)
            {
                WriteLiteral(stringValue);
            }
            else
            {
                Write(stringValue);
            }
        }
        else if (isLiteral)
        {
            WriteLiteral(value);
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

    /// <inheritdoc />
    public abstract void BeginContext(int position, int length, bool isLiteral);

    /// <inheritdoc />
    public abstract void EndContext();

    private static bool IsBoolFalseOrNullValue(string? prefix, object? value)
    {
        return string.IsNullOrEmpty(prefix) &&
            (value is null ||
            (value is bool boolValue && !boolValue));
    }

    private static bool IsBoolTrueWithEmptyPrefixValue(string? prefix, object? value)
    {
        // If the value is just the bool 'true', use the attribute name as the value.
        return string.IsNullOrEmpty(prefix) &&
            (value is bool boolValue && boolValue);
    }

    /// <inheritdoc />
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

    private readonly struct TagHelperScopeInfo
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
