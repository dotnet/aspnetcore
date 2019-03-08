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
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    public abstract class RazorPageBase : RazorRazorPageBase<ViewContext>, IRazorPage
    {
        private StringWriter _valueBuffer;
        private ITagHelperFactory _tagHelperFactory;
        private IViewBufferScope _bufferScope;
        private TextWriter _pageWriter;
        private IUrlHelper _urlHelper;

        /// <summary>
        /// Gets or sets a <see cref="System.Diagnostics.DiagnosticSource"/> instance used to instrument the page execution.
        /// </summary>
        [RazorInject]
        public virtual DiagnosticSource DiagnosticSource { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        [RazorInject]
        public override HtmlEncoder HtmlEncoder { get; set; }

        /// <summary>
        /// Gets the <see cref="ClaimsPrincipal"/> of the current logged in user.
        /// </summary>
        public virtual ClaimsPrincipal User => ViewContext?.HttpContext?.User;

        /// <summary>
        /// Gets the <see cref="ITempDataDictionary"/> from the <see cref="ViewContext"/>.
        /// </summary>
        /// <remarks>Returns null if <see cref="ViewContext"/> is null.</remarks>
        public ITempDataDictionary TempData => ViewContext?.TempData;

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
        /// All writes to the <see cref="RazorRazorPageBase{TOutputContext}.Output"/> or <see cref="ViewContext.Writer"/> after calling this method will
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
        /// All writes to the <see cref="RazorRazorPageBase{TOutputContext}.Output"/> or <see cref="ViewContext.Writer"/> after calling this method will
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

            var content = _valueBuffer.ToString();
            _valueBuffer.GetStringBuilder().Clear();

            // Restore previous writer.
            ViewContext.Writer = _pageWriter;
            _pageWriter = null;

            return content;
        }

        public override string Href(string contentPath)
        {
            if (contentPath == null)
            {
                throw new ArgumentNullException(nameof(contentPath));
            }

            if (_urlHelper == null)
            {
                var viewContext = ViewContext;
                var services = viewContext?.HttpContext.RequestServices;
                var factory = services.GetRequiredService<IUrlHelperFactory>();
                _urlHelper = factory.GetUrlHelper(viewContext);
            }

            return _urlHelper.Content(contentPath);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="RazorRazorPageBase{TOutputContext}.Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to write.</param>
        public override void Write(object value)
        {
            if (value == null || value == HtmlString.Empty)
            {
                return;
            }

            var writer = Output;
            var encoder = HtmlEncoder;
            if (value is IHtmlContent htmlContent)
            {
                var bufferedWriter = writer as ViewBufferTextWriter;
                if (bufferedWriter == null || !bufferedWriter.IsBuffering)
                {
                    htmlContent.WriteTo(writer, encoder);
                }
                else
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

                return;
            }

            Write(value.ToString());
        }

        public override Task<HtmlString> FlushAsync()
        {
            // If there are active scopes, then we should throw. Cannot flush content that has the potential to change.
            if (TagHelperScopes.Count > 0)
            {
                throw new InvalidOperationException(
                    Resources.FormatRazorPage_CannotFlushWhileInAWritingScope(nameof(FlushAsync), Path));
            }

            return base.FlushAsync();
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
            var antiforgery = viewContext?.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
            antiforgery.SetCookieTokenAndHeader(viewContext?.HttpContext);

            return HtmlString.Empty;
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
}