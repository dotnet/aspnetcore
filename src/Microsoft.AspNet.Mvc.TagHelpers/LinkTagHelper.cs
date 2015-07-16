// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Logging;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;link&gt; elements that supports fallback href paths.
    /// </summary>
    /// <remarks>
    /// The tag helper won't process for cases with just the 'href' attribute.
    /// </remarks>
    [TargetElement("link", Attributes = HrefIncludeAttributeName)]
    [TargetElement("link", Attributes = HrefExcludeAttributeName)]
    [TargetElement("link", Attributes = FallbackHrefAttributeName)]
    [TargetElement("link", Attributes = FallbackHrefIncludeAttributeName)]
    [TargetElement("link", Attributes = FallbackHrefExcludeAttributeName)]
    [TargetElement("link", Attributes = FallbackTestClassAttributeName)]
    [TargetElement("link", Attributes = FallbackTestPropertyAttributeName)]
    [TargetElement("link", Attributes = FallbackTestValueAttributeName)]
    [TargetElement("link", Attributes = AppendVersionAttributeName)]
    public class LinkTagHelper : TagHelper
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
        /// <param name="htmlEncoder">The <see cref="IHtmlEncoder"/>.</param>
        /// <param name="javaScriptEncoder">The <see cref="IJavaScriptStringEncoder"/>.</param>
        public LinkTagHelper(
            ILogger<LinkTagHelper> logger,
            IHostingEnvironment hostingEnvironment,
            IMemoryCache cache,
            IHtmlEncoder htmlEncoder,
            IJavaScriptStringEncoder javaScriptEncoder)
        {
            Logger = logger;
            HostingEnvironment = hostingEnvironment;
            Cache = cache;
            HtmlEncoder = htmlEncoder;
            JavaScriptEncoder = javaScriptEncoder;
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

        protected IHtmlEncoder HtmlEncoder { get; }

        protected IJavaScriptStringEncoder JavaScriptEncoder { get; }

        // Internal for ease of use when testing.
        protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Pass through attribute that is also a well-known HTML attribute.
            if (Href != null)
            {
                output.CopyHtmlAttribute(HrefAttributeName, context);
            }

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

                var attributeStringValue = output.Attributes[HrefAttributeName]?.Value as string;
                if (attributeStringValue != null)
                {
                    output.Attributes[HrefAttributeName].Value =
                        _fileVersionProvider.AddFileVersionToPath(attributeStringValue);
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
                    output.Content.SetContent(string.Empty);
                }
            }

            if (mode == Mode.Fallback)
            {
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
                    for (var i=0; i < fallbackHrefs.Length; i++)
                    {
                        // fallbackHrefs come from bound attributes and globbing. Must always be non-null.
                        Debug.Assert(fallbackHrefs[i] != null);

                        fallbackHrefs[i] = _fileVersionProvider.AddFileVersionToPath(fallbackHrefs[i]);
                    }
                }

                builder.Append(Environment.NewLine);

                // Build the <meta /> tag that's used to test for the presence of the stylesheet
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "<meta name=\"x-stylesheet-fallback-test\" class=\"{0}\" />",
                    HtmlEncoder.HtmlEncode(FallbackTestClass));

                // Build the <script /> tag that checks the effective style of <meta /> tag above and renders the extra
                // <link /> tag to load the fallback stylesheet if the test CSS property value is found to be false,
                // indicating that the primary stylesheet failed to load.
                builder
                    .Append("<script>")
                    .AppendFormat(
                        CultureInfo.InvariantCulture,
                        JavaScriptResources.GetEmbeddedJavaScript(FallbackJavaScriptResourceName),
                        JavaScriptEncoder.JavaScriptStringEncode(FallbackTestProperty),
                        JavaScriptEncoder.JavaScriptStringEncode(FallbackTestValue),
                        JavaScriptStringArrayEncoder.Encode(JavaScriptEncoder, fallbackHrefs))
                    .Append("</script>");
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
            builder.Append("<link ");

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
                    .Append(attribute.Name)
                    .Append("=\"")
                    .Append(HtmlEncoder, ViewContext.Writer.Encoding, attributeValue)
                    .Append("\" ");
            }

            builder.Append("/>");
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