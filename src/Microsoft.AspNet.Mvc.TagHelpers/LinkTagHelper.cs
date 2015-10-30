// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor.TagHelpers;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;link&gt; elements that supports fallback href paths.
    /// </summary>
    /// <remarks>
    /// The tag helper won't process for cases with just the 'href' attribute.
    /// </remarks>
    [HtmlTargetElement("link", Attributes = HrefIncludeAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = HrefExcludeAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackHrefAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackHrefIncludeAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackHrefExcludeAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackTestClassAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackTestPropertyAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = FallbackTestValueAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = AppendVersionAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class LinkTagHelper : UrlResolutionTagHelper
    {
        private static readonly string Namespace = typeof(LinkTagHelper).Namespace;

        private const string HrefIncludeAttributeName = "asp-href-include";
        private const string HrefExcludeAttributeName = "asp-href-exclude";
        private const string FallbackHrefAttributeName = "asp-fallback-href";
        private const string FallbackHrefIncludeAttributeName = "asp-fallback-href-include";
        private const string FallbackHrefExcludeAttributeName = "asp-fallback-href-exclude";
        private const string FallbackTestClassAttributeName = "asp-fallback-test-class";
        private const string FallbackTestPropertyAttributeName = "asp-fallback-test-property";
        private const string FallbackTestValueAttributeName = "asp-fallback-test-value";
        private readonly string FallbackJavaScriptResourceName = Namespace + ".compiler.resources.LinkTagHelper_FallbackJavaScript.js";
        private const string AppendVersionAttributeName = "asp-append-version";
        private const string HrefAttributeName = "href";

        private FileVersionProvider _fileVersionProvider;

        private static readonly ModeAttributes<Mode>[] ModeDetails = new[] {
            // Regular src with file version alone
            ModeAttributes.Create(Mode.AppendVersion, new[] { AppendVersionAttributeName }),
            // Globbed Href (include only) no static href
            ModeAttributes.Create(Mode.GlobbedHref, new [] { HrefIncludeAttributeName }),
            // Globbed Href (include & exclude), no static href
            ModeAttributes.Create(Mode.GlobbedHref, new [] { HrefIncludeAttributeName, HrefExcludeAttributeName }),
            // Fallback with static href
            ModeAttributes.Create(
                Mode.Fallback, new[]
                {
                    FallbackHrefAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
            // Fallback with globbed href (include only)
            ModeAttributes.Create(
                Mode.Fallback, new[] {
                    FallbackHrefIncludeAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
            // Fallback with globbed href (include & exclude)
            ModeAttributes.Create(
                Mode.Fallback, new[] {
                    FallbackHrefIncludeAttributeName,
                    FallbackHrefExcludeAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
        };

        /// <summary>
        /// Creates a new <see cref="LinkTagHelper"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{ScriptTagHelper}"/>.</param>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
        /// <param name="cache">The <see cref="IMemoryCache"/>.</param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        /// <param name="javaScriptEncoder">The <see cref="JavaScriptEncoder"/>.</param>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        public LinkTagHelper(
            ILogger<LinkTagHelper> logger,
            IHostingEnvironment hostingEnvironment,
            IMemoryCache cache,
            HtmlEncoder htmlEncoder,
            JavaScriptEncoder javaScriptEncoder,
            IUrlHelper urlHelper)
            : base(urlHelper, htmlEncoder)
        {
            Logger = logger;
            HostingEnvironment = hostingEnvironment;
            Cache = cache;
            JavaScriptEncoder = javaScriptEncoder;
        }

        /// <inheritdoc />
        public override int Order
        {
            get
            {
                return -1000;
            }
        }

        /// <summary>
        /// Address of the linked resource.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases.
        /// </remarks>
        [HtmlAttributeName(HrefAttributeName)]
        public string Href { get; set; }

        /// <summary>
        /// A comma separated list of globbed file patterns of CSS stylesheets to load.
        /// The glob patterns are assessed relative to the application's 'webroot' setting.
        /// </summary>
        [HtmlAttributeName(HrefIncludeAttributeName)]
        public string HrefInclude { get; set; }

        /// <summary>
        /// A comma separated list of globbed file patterns of CSS stylesheets to exclude from loading.
        /// The glob patterns are assessed relative to the application's 'webroot' setting.
        /// Must be used in conjunction with <see cref="HrefInclude"/>.
        /// </summary>
        [HtmlAttributeName(HrefExcludeAttributeName)]
        public string HrefExclude { get; set; }

        /// <summary>
        /// The URL of a CSS stylesheet to fallback to in the case the primary one fails.
        /// </summary>
        [HtmlAttributeName(FallbackHrefAttributeName)]
        public string FallbackHref { get; set; }

        /// <summary>
        /// Value indicating if file version should be appended to the href urls.
        /// </summary>
        /// <remarks>
        /// If <c>true</c> then a query string "v" with the encoded content of the file is added.
        /// </remarks>
        [HtmlAttributeName(AppendVersionAttributeName)]
        public bool? AppendVersion { get; set; }

        /// <summary>
        /// A comma separated list of globbed file patterns of CSS stylesheets to fallback to in the case the primary
        /// one fails.
        /// The glob patterns are assessed relative to the application's 'webroot' setting.
        /// </summary>
        [HtmlAttributeName(FallbackHrefIncludeAttributeName)]
        public string FallbackHrefInclude { get; set; }

        /// <summary>
        /// A comma separated list of globbed file patterns of CSS stylesheets to exclude from the fallback list, in
        /// the case the primary one fails.
        /// The glob patterns are assessed relative to the application's 'webroot' setting.
        /// Must be used in conjunction with <see cref="FallbackHrefInclude"/>.
        /// </summary>
        [HtmlAttributeName(FallbackHrefExcludeAttributeName)]
        public string FallbackHrefExclude { get; set; }

        /// <summary>
        /// The class name defined in the stylesheet to use for the fallback test.
        /// Must be used in conjunction with <see cref="FallbackTestProperty"/> and <see cref="FallbackTestValue"/>,
        /// and either <see cref="FallbackHref"/> or <see cref="FallbackHrefInclude"/>.
        /// </summary>
        [HtmlAttributeName(FallbackTestClassAttributeName)]
        public string FallbackTestClass { get; set; }

        /// <summary>
        /// The CSS property name to use for the fallback test.
        /// Must be used in conjunction with <see cref="FallbackTestClass"/> and <see cref="FallbackTestValue"/>,
        /// and either <see cref="FallbackHref"/> or <see cref="FallbackHrefInclude"/>.
        /// </summary>
        [HtmlAttributeName(FallbackTestPropertyAttributeName)]
        public string FallbackTestProperty { get; set; }

        /// <summary>
        /// The CSS property value to use for the fallback test.
        /// Must be used in conjunction with <see cref="FallbackTestClass"/> and <see cref="FallbackTestProperty"/>,
        /// and either <see cref="FallbackHref"/> or <see cref="FallbackHrefInclude"/>.
        /// </summary>
        [HtmlAttributeName(FallbackTestValueAttributeName)]
        public string FallbackTestValue { get; set; }

        protected ILogger<LinkTagHelper> Logger { get; }

        protected IHostingEnvironment HostingEnvironment { get; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected IMemoryCache Cache { get; }

        protected JavaScriptEncoder JavaScriptEncoder { get; }

        // Internal for ease of use when testing.
        protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            // Pass through attribute that is also a well-known HTML attribute.
            if (Href != null)
            {
                output.CopyHtmlAttribute(HrefAttributeName, context);
            }

            // If there's no "href" attribute in output.Attributes this will noop.
            ProcessUrlAttribute(HrefAttributeName, output);

            // Retrieve the TagHelperOutput variation of the "href" attribute in case other TagHelpers in the
            // pipeline have touched the value. If the value is already encoded this LinkTagHelper may
            // not function properly.
            Href = output.Attributes[HrefAttributeName]?.Value as string;

            var modeResult = AttributeMatcher.DetermineMode(context, ModeDetails);

            modeResult.LogDetails(Logger, this, context.UniqueId, ViewContext.View.Path);

            if (!modeResult.FullMatches.Any())
            {
                // No attributes matched so we have nothing to do
                return;
            }

            // NOTE: Values in TagHelperOutput.Attributes may already be HTML-encoded.
            var attributes = new TagHelperAttributeList(output.Attributes);

            if (AppendVersion == true)
            {
                EnsureFileVersionProvider();

                if (Href != null)
                {
                    output.Attributes[HrefAttributeName].Value = _fileVersionProvider.AddFileVersionToPath(Href);
                }
            }

            var builder = new DefaultTagHelperContent();

            // Get the highest matched mode
            var mode = modeResult.FullMatches.Select(match => match.Mode).Max();

            if (mode == Mode.GlobbedHref || mode == Mode.Fallback && !string.IsNullOrEmpty(HrefInclude))
            {
                BuildGlobbedLinkTags(attributes, builder);
                if (string.IsNullOrEmpty(Href))
                {
                    // Only HrefInclude is specified. Don't render the original tag.
                    output.TagName = null;
                    output.Content.SetContent(HtmlString.Empty);
                }
            }

            if (mode == Mode.Fallback)
            {
                string resolvedUrl;
                if (TryResolveUrl(FallbackHref, encodeWebRoot: false, resolvedUrl: out resolvedUrl))
                {
                    FallbackHref = resolvedUrl;
                }

                BuildFallbackBlock(builder);
            }

            output.PostElement.SetContent(builder);
        }

        private void BuildGlobbedLinkTags(TagHelperAttributeList attributes, TagHelperContent builder)
        {
            EnsureGlobbingUrlBuilder();

            // Build a <link /> tag for each matched href.
            var urls = GlobbingUrlBuilder.BuildUrlList(null, HrefInclude, HrefExclude);
            foreach (var url in urls)
            {
                // "url" values come from bound attributes and globbing. Must always be non-null.
                Debug.Assert(url != null);

                if (string.Equals(Href, url, StringComparison.OrdinalIgnoreCase))
                {
                    // Don't build duplicate link tag for the original href url.
                    continue;
                }

                attributes[HrefAttributeName] = url;
                BuildLinkTag(attributes, builder);
            }
        }

        private void BuildFallbackBlock(TagHelperContent builder)
        {
            EnsureGlobbingUrlBuilder();
            var fallbackHrefs =
                GlobbingUrlBuilder.BuildUrlList(FallbackHref, FallbackHrefInclude, FallbackHrefExclude).ToArray();

            if (fallbackHrefs.Length > 0)
            {
                if (AppendVersion == true)
                {
                    for (var i = 0; i < fallbackHrefs.Length; i++)
                    {
                        // fallbackHrefs come from bound attributes and globbing. Must always be non-null.
                        Debug.Assert(fallbackHrefs[i] != null);

                        fallbackHrefs[i] = _fileVersionProvider.AddFileVersionToPath(fallbackHrefs[i]);
                    }
                }

                builder.Append(HtmlString.NewLine);

                // Build the <meta /> tag that's used to test for the presence of the stylesheet
                builder
                    .AppendHtml("<meta name=\"x-stylesheet-fallback-test\" class=\"")
                    .Append(FallbackTestClass)
                    .AppendHtml("\" />");

                // Build the <script /> tag that checks the effective style of <meta /> tag above and renders the extra
                // <link /> tag to load the fallback stylesheet if the test CSS property value is found to be false,
                // indicating that the primary stylesheet failed to load.
                builder
                    .AppendHtml("<script>")
                    .AppendHtml(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            JavaScriptResources.GetEmbeddedJavaScript(FallbackJavaScriptResourceName),
                            JavaScriptEncoder.Encode(FallbackTestProperty),
                            JavaScriptEncoder.Encode(FallbackTestValue),
                            JavaScriptStringArrayEncoder.Encode(JavaScriptEncoder, fallbackHrefs)))
                    .AppendHtml("</script>");
            }
        }

        private void EnsureGlobbingUrlBuilder()
        {
            if (GlobbingUrlBuilder == null)
            {
                GlobbingUrlBuilder = new GlobbingUrlBuilder(
                    HostingEnvironment.WebRootFileProvider,
                    Cache,
                    ViewContext.HttpContext.Request.PathBase);
            }
        }

        private void EnsureFileVersionProvider()
        {
            if (_fileVersionProvider == null)
            {
                _fileVersionProvider = new FileVersionProvider(
                    HostingEnvironment.WebRootFileProvider,
                    Cache,
                    ViewContext.HttpContext.Request.PathBase);
            }
        }

        private void BuildLinkTag(TagHelperAttributeList attributes, TagHelperContent builder)
        {
            builder.AppendHtml("<link ");

            foreach (var attribute in attributes)
            {
                var attributeValue = attribute.Value;
                if (AppendVersion == true &&
                    string.Equals(attribute.Name, HrefAttributeName, StringComparison.OrdinalIgnoreCase))
                {
                    // "href" values come from bound attributes and globbing. So anything but a non-null string is
                    // unexpected but could happen if another helper targeting the same element does something odd.
                    // Pass through existing value in that case.
                    var attributeStringValue = attributeValue as string;
                    if (attributeStringValue != null)
                    {
                        attributeValue = _fileVersionProvider.AddFileVersionToPath(attributeStringValue);
                    }
                }

                builder
                    .AppendHtml(attribute.Name)
                    .AppendHtml("=\"")
                    .Append(HtmlEncoder, ViewContext.Writer.Encoding, attributeValue)
                    .AppendHtml("\" ");
            }

            builder.AppendHtml("/>");
        }

        private enum Mode
        {
            /// <summary>
            /// Just adding a file version for the generated urls.
            /// </summary>
            AppendVersion = 0,

            /// <summary>
            /// Just performing file globbing search for the href, rendering a separate &lt;link&gt; for each match.
            /// </summary>
            GlobbedHref = 1,

            /// <summary>
            /// Rendering a fallback block if primary stylesheet fails to load. Will also do globbing for both the
            /// primary and fallback hrefs if the appropriate properties are set.
            /// </summary>
            Fallback = 2,
        }
    }
}