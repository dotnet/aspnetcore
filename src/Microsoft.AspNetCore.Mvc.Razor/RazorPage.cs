// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    public abstract class RazorPage : RazorPageBase, IRazorPage
    {
        private readonly HashSet<string> _renderedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<TagHelperScopeInfo> _tagHelperScopes = new Stack<TagHelperScopeInfo>();
        private IUrlHelper _urlHelper;
        private ITagHelperFactory _tagHelperFactory;
        private bool _renderedBody;
        private StringWriter _valueBuffer;
        private IViewBufferScope _bufferScope;
        private bool _ignoreBody;
        private HashSet<string> _ignoredSections;
        private TextWriter _pageWriter;

        public RazorPage()
        {
            SectionWriters = new Dictionary<string, RenderAsyncDelegate>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// An <see cref="HttpContext"/> representing the current request execution.
        /// </summary>
        public HttpContext Context => ViewContext?.HttpContext;

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
        public string Layout { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        [RazorInject]
        public HtmlEncoder HtmlEncoder { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="DiagnosticSource.DiagnosticSource"/> instance used to instrument the page execution.
        /// </summary>
        [RazorInject]
        public DiagnosticSource DiagnosticSource { get; set; }

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

        /// <summary>
        /// Gets the <see cref="ClaimsPrincipal"/> of the current logged in user.
        /// </summary>
        public virtual ClaimsPrincipal User => Context?.User;

        /// <summary>
        /// Gets the dynamic view data dictionary.
        /// </summary>
        public dynamic ViewBag => ViewContext?.ViewBag;

        /// <summary>
        /// Gets the <see cref="ITempDataDictionary"/> from the <see cref="ViewContext"/>.
        /// </summary>
        /// <remarks>Returns null if <see cref="ViewContext"/> is null.</remarks>
        public ITempDataDictionary TempData => ViewContext?.TempData;

        /// <inheritdoc />
        public IHtmlContent BodyContent { get; set; }

        /// <inheritdoc />
        public bool IsLayoutBeingRendered { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters { get; set; }

        /// <inheritdoc />
        public IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

        protected override TextWriter Writer => Output;

        protected override HtmlEncoder Encoder => HtmlEncoder;

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
        public static string InvalidTagHelperIndexerAssignment(
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
        /// All writes to the <see cref="Writer"/> or <see cref="ViewContext.Writer"/> after calling this method will
        /// be buffered until <see cref="EndTagHelperWritingScope"/> is called.
        /// </remarks>
        public void StartTagHelperWritingScope(HtmlEncoder encoder)
        {
            var buffer = new ViewBuffer(BufferScope, Path, ViewBuffer.TagHelperPageSize);
            _tagHelperScopes.Push(new TagHelperScopeInfo(buffer, HtmlEncoder, ViewContext.Writer));

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
            if (_tagHelperScopes.Count == 0)
            {
                throw new InvalidOperationException(Resources.RazorPage_ThereIsNoActiveWritingScopeToEnd);
            }

            var scopeInfo = _tagHelperScopes.Pop();

            // Get the content written during the current scope.
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.AppendHtml(scopeInfo.Buffer);

            // Restore previous scope.
            HtmlEncoder = scopeInfo.Encoder;
            ViewContext.Writer = scopeInfo.Writer;

            return tagHelperContent;
        }

        /// <summary>
        /// Starts a new scope for writing <see cref="ITagHelper"/> attribute values.
        /// </summary>
        /// <remarks>
        /// All writes to the <see cref="Writer"/> or <see cref="ViewContext.Writer"/> after calling this method will
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

        public override string Href(string contentPath)
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
        /// In a Razor layout page, renders the portion of a content page that is not within a named section.
        /// </summary>
        /// <returns>The HTML content to render.</returns>
        protected virtual IHtmlContent RenderBody()
        {
            if (BodyContent == null)
            {
                var message = Resources.FormatRazorPage_MethodCannotBeCalled(nameof(RenderBody), Path);
                throw new InvalidOperationException(message);
            }

            _renderedBody = true;
            return BodyContent;
        }

        /// <summary>
        /// In a Razor layout page, ignores rendering the portion of a content page that is not within a named section.
        /// </summary>
        public void IgnoreBody()
        {
            _ignoreBody = true;
        }

        /// <summary>
        /// Creates a named content section in the page that can be invoked in a Layout page using
        /// <see cref="RenderSection(string)"/> or <see cref="RenderSectionAsync(string, bool)"/>.
        /// </summary>
        /// <param name="name">The name of the section to create.</param>
        /// <param name="section">The <see cref="RenderAsyncDelegate"/> to execute when rendering the section.</param>
        public override void DefineSection(string name, RenderAsyncDelegate section)
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
        /// Returns a value that indicates whether the specified section is defined in the content page.
        /// </summary>
        /// <param name="name">The section name to search for.</param>
        /// <returns><c>true</c> if the specified section is defined in the content page; otherwise, <c>false</c>.</returns>
        public bool IsSectionDefined(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            EnsureMethodCanBeInvoked(nameof(IsSectionDefined));
            return PreviousSectionWriters.ContainsKey(name);
        }

        /// <summary>
        /// In layout pages, renders the content of the section named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the section to render.</param>
        /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks>The method writes to the <see cref="Writer"/> and the value returned is a token
        /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
        /// value does not represent the rendered content.</remarks>
        public HtmlString RenderSection(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return RenderSection(name, required: true);
        }

        /// <summary>
        /// In layout pages, renders the content of the section named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The section to render.</param>
        /// <param name="required">Indicates if this section must be rendered.</param>
        /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks>The method writes to the <see cref="Writer"/> and the value returned is a token
        /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
        /// value does not represent the rendered content.</remarks>
        public HtmlString RenderSection(string name, bool required)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            EnsureMethodCanBeInvoked(nameof(RenderSection));

            var task = RenderSectionAsyncCore(name, required);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// In layout pages, asynchronously renders the content of the section named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The section to render.</param>
        /// <returns>
        /// A <see cref="Task{HtmlString}"/> that on completion returns an empty <see cref="IHtmlContent"/>.
        /// </returns>
        /// <remarks>The method writes to the <see cref="Writer"/> and the value returned is a token
        /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
        /// value does not represent the rendered content.</remarks>
        public Task<HtmlString> RenderSectionAsync(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return RenderSectionAsync(name, required: true);
        }

        /// <summary>
        /// In layout pages, asynchronously renders the content of the section named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The section to render.</param>
        /// <param name="required">Indicates the <paramref name="name"/> section must be registered
        /// (using <c>@section</c>) in the page.</param>
        /// <returns>
        /// A <see cref="Task{HtmlString}"/> that on completion returns an empty <see cref="IHtmlContent"/>.
        /// </returns>
        /// <remarks>The method writes to the <see cref="Writer"/> and the value returned is a token
        /// value that allows the Write (produced due to @RenderSection(..)) to succeed. However the
        /// value does not represent the rendered content.</remarks>
        /// <exception cref="InvalidOperationException">if <paramref name="required"/> is <c>true</c> and the section
        /// was not registered using the <c>@section</c> in the Razor page.</exception>
        public Task<HtmlString> RenderSectionAsync(string name, bool required)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            EnsureMethodCanBeInvoked(nameof(RenderSectionAsync));
            return RenderSectionAsyncCore(name, required);
        }

        private async Task<HtmlString> RenderSectionAsyncCore(string sectionName, bool required)
        {
            if (_renderedSections.Contains(sectionName))
            {
                var message = Resources.FormatSectionAlreadyRendered(nameof(RenderSectionAsync), Path, sectionName);
                throw new InvalidOperationException(message);
            }

            RenderAsyncDelegate renderDelegate;
            if (PreviousSectionWriters.TryGetValue(sectionName, out renderDelegate))
            {
                _renderedSections.Add(sectionName);
                
                await renderDelegate();

                // Return a token value that allows the Write call that wraps the RenderSection \ RenderSectionAsync
                // to succeed.
                return HtmlString.Empty;
            }
            else if (required)
            {
                // If the section is not found, and it is not optional, throw an error.
                var message = Resources.FormatSectionNotDefined(
                    ViewContext.ExecutingFilePath,
                    sectionName,
                    ViewContext.View.Path);
                throw new InvalidOperationException(message);
            }
            else
            {
                // If the section is optional and not found, then don't do anything.
                return null;
            }
        }

        /// <summary>
        /// In layout pages, ignores rendering the content of the section named <paramref name="sectionName"/>.
        /// </summary>
        /// <param name="sectionName">The section to ignore.</param>
        public void IgnoreSection(string sectionName)
        {
            if (sectionName == null)
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            if (!PreviousSectionWriters.ContainsKey(sectionName))
            {
                // If the section is not defined, throw an error.
                throw new InvalidOperationException(Resources.FormatSectionNotDefined(
                    ViewContext.ExecutingFilePath,
                    sectionName,
                    ViewContext.View.Path));
            }

            if (_ignoredSections == null)
            {
                _ignoredSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            _ignoredSections.Add(sectionName);
        }

        /// <summary>
        /// Invokes <see cref="TextWriter.FlushAsync"/> on <see cref="Writer"/> and <see cref="M:Stream.FlushAsync"/>
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
        public async Task<HtmlString> FlushAsync()
        {
            // If there are active scopes, then we should throw. Cannot flush content that has the potential to change.
            if (_tagHelperScopes.Count > 0)
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

            await Writer.FlushAsync();
            await Context.Response.Body.FlushAsync();
            return HtmlString.Empty;
        }

        /// <inheritdoc />
        public void EnsureRenderedBodyOrSections()
        {
            // a) all sections defined for this page are rendered.
            // b) if no sections are defined, then the body is rendered if it's available.
            if (PreviousSectionWriters != null && PreviousSectionWriters.Count > 0)
            {
                var sectionsNotRendered = PreviousSectionWriters.Keys.Except(
                    _renderedSections,
                    StringComparer.OrdinalIgnoreCase);

                string[] sectionsNotIgnored;
                if (_ignoredSections != null)
                {
                    sectionsNotIgnored = sectionsNotRendered.Except(_ignoredSections, StringComparer.OrdinalIgnoreCase).ToArray();
                }
                else
                {
                    sectionsNotIgnored = sectionsNotRendered.ToArray();
                }

                if (sectionsNotIgnored.Length > 0)
                {
                    var sectionNames = string.Join(", ", sectionsNotIgnored);
                    throw new InvalidOperationException(Resources.FormatSectionsNotRendered(Path, sectionNames, nameof(IgnoreSection)));
                }
            }
            else if (BodyContent != null && !_renderedBody && !_ignoreBody)
            {
                // There are no sections defined, but RenderBody was NOT called.
                // If a body was defined and the body not ignored, then RenderBody should have been called.
                var message = Resources.FormatRenderBodyNotCalled(nameof(RenderBody), Path, nameof(IgnoreBody));
                throw new InvalidOperationException(message);
            }
        }

        public override void BeginContext(int position, int length, bool isLiteral)
        {
            const string BeginContextEvent = "Microsoft.AspNetCore.Mvc.Razor.BeginInstrumentationContext";

            if (DiagnosticSource?.IsEnabled(BeginContextEvent) == true)
            {
                DiagnosticSource.Write(
                    BeginContextEvent,
                    new
                    {
                        httpContext = Context,
                        path = Path,
                        position = position,
                        length = length,
                        isLiteral = isLiteral,
                    });
            }
        }

        public override void EndContext()
        {
            const string EndContextEvent = "Microsoft.AspNetCore.Mvc.Razor.EndInstrumentationContext";

            if (DiagnosticSource?.IsEnabled(EndContextEvent) == true)
            {
                DiagnosticSource.Write(
                    EndContextEvent,
                    new
                    {
                        httpContext = Context,
                        path = Path,
                    });
            }
        }

        /// <summary>
        /// Sets antiforgery cookie and X-Frame-Options header on the response.
        /// </summary>
        /// <returns>An empty <see cref="IHtmlContent"/>.</returns>
        /// <remarks> Call this method to send antiforgery cookie token and X-Frame-Options header to client
        /// before <see cref="FlushAsync"/> flushes the headers. </remarks>
        public virtual HtmlString SetAntiforgeryCookieAndHeader()
        {
            var antiforgery = Context.RequestServices.GetRequiredService<IAntiforgery>();
            antiforgery.SetCookieTokenAndHeader(Context);

            return HtmlString.Empty;
        }

        private void EnsureMethodCanBeInvoked(string methodName)
        {
            if (PreviousSectionWriters == null)
            {
                throw new InvalidOperationException(Resources.FormatRazorPage_MethodCannotBeCalled(methodName, Path));
            }
        }

        private struct TagHelperScopeInfo
        {
            public TagHelperScopeInfo(ViewBuffer buffer, HtmlEncoder encoder, TextWriter writer)
            {
                Buffer = buffer;
                Encoder = encoder;
                Writer = writer;
            }

            public ViewBuffer Buffer { get; }

            public HtmlEncoder Encoder { get; }

            public TextWriter Writer { get; }
        }
    }
}