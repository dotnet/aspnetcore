// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

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
    private static readonly string FallbackJavaScriptResourceName =
        typeof(LinkTagHelper).Namespace + ".compiler.resources.LinkTagHelper_FallbackJavaScript.js";

    private const string HrefIncludeAttributeName = "asp-href-include";
    private const string HrefExcludeAttributeName = "asp-href-exclude";
    private const string FallbackHrefAttributeName = "asp-fallback-href";
    private const string SuppressFallbackIntegrityAttributeName = "asp-suppress-fallback-integrity";
    private const string FallbackHrefIncludeAttributeName = "asp-fallback-href-include";
    private const string FallbackHrefExcludeAttributeName = "asp-fallback-href-exclude";
    private const string FallbackTestClassAttributeName = "asp-fallback-test-class";
    private const string FallbackTestPropertyAttributeName = "asp-fallback-test-property";
    private const string FallbackTestValueAttributeName = "asp-fallback-test-value";
    private const string AppendVersionAttributeName = "asp-append-version";
    private const string HrefAttributeName = "href";
    private const string RelAttributeName = "rel";
    private const string IntegrityAttributeName = "integrity";
    private static readonly Func<Mode, Mode, int> Compare = (a, b) => a - b;

    private static readonly ModeAttributes<Mode>[] ModeDetails = new[] {
            // Regular src with file version alone
            new ModeAttributes<Mode>(Mode.AppendVersion, new[] { AppendVersionAttributeName }),
            // Globbed Href (include only) no static href
            new ModeAttributes<Mode>(Mode.GlobbedHref, new [] { HrefIncludeAttributeName }),
            // Globbed Href (include & exclude), no static href
            new ModeAttributes<Mode>(Mode.GlobbedHref, new [] { HrefIncludeAttributeName, HrefExcludeAttributeName }),
            // Fallback with static href
            new ModeAttributes<Mode>(
                Mode.Fallback,
                new[]
                {
                    FallbackHrefAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
            // Fallback with globbed href (include only)
            new ModeAttributes<Mode>(
                Mode.Fallback,
                new[]
                {
                    FallbackHrefIncludeAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
            // Fallback with globbed href (include & exclude)
            new ModeAttributes<Mode>(
                Mode.Fallback,
                new[]
                {
                    FallbackHrefIncludeAttributeName,
                    FallbackHrefExcludeAttributeName,
                    FallbackTestClassAttributeName,
                    FallbackTestPropertyAttributeName,
                    FallbackTestValueAttributeName
                }),
        };
    private StringWriter _stringWriter;

    /// <summary>
    /// Creates a new <see cref="LinkTagHelper"/>.
    /// </summary>
    /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
    /// <param name="cacheProvider"></param>
    /// <param name="fileVersionProvider">The <see cref="IFileVersionProvider"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    /// <param name="javaScriptEncoder">The <see cref="JavaScriptEncoder"/>.</param>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    // Decorated with ActivatorUtilitiesConstructor since we want to influence tag helper activation
    // to use this constructor in the default case.
    public LinkTagHelper(
        IWebHostEnvironment hostingEnvironment,
        TagHelperMemoryCacheProvider cacheProvider,
        IFileVersionProvider fileVersionProvider,
        HtmlEncoder htmlEncoder,
        JavaScriptEncoder javaScriptEncoder,
        IUrlHelperFactory urlHelperFactory)
        : base(urlHelperFactory, htmlEncoder)
    {
        HostingEnvironment = hostingEnvironment;
        JavaScriptEncoder = javaScriptEncoder;
        Cache = cacheProvider.Cache;
        FileVersionProvider = fileVersionProvider;
    }

    /// <inheritdoc />
    public override int Order => -1000;

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
    /// Boolean value that determines if an integrity hash will be compared with <see cref="FallbackHref"/> value.
    /// </summary>
    [HtmlAttributeName(SuppressFallbackIntegrityAttributeName)]
    public bool SuppressFallbackIntegrity { get; set; }

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

    /// <summary>
    /// Gets the <see cref="IWebHostEnvironment"/> for the application.
    /// </summary>
    protected internal IWebHostEnvironment HostingEnvironment { get; }

    /// <summary>
    /// Gets the <see cref="IMemoryCache"/> used to store globbed urls.
    /// </summary>
    protected internal IMemoryCache Cache { get; }

    /// <summary>
    /// Gets the <see cref="System.Text.Encodings.Web.JavaScriptEncoder"/> used to encode fallback information.
    /// </summary>
    protected JavaScriptEncoder JavaScriptEncoder { get; }

    /// <summary>
    /// Gets the <see cref="GlobbingUrlBuilder"/> used to populate included and excluded urls.
    /// </summary>
    // Internal for ease of use when testing.
    protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }

    internal IFileVersionProvider FileVersionProvider { get; private set; }

    // Shared writer for determining the string content of a TagHelperAttribute's Value.
    private StringWriter StringWriter
    {
        get
        {
            if (_stringWriter == null)
            {
                _stringWriter = new StringWriter();
            }

            return _stringWriter;
        }
    }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

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

        if (!AttributeMatcher.TryDetermineMode(context, ModeDetails, Compare, out var mode))
        {
            // No attributes matched so we have nothing to do
            return;
        }

        if (AppendVersion == true)
        {
            EnsureFileVersionProvider();

            if (Href != null)
            {
                var href = GetVersionedResourceUrl(Href);
                var index = output.Attributes.IndexOfName(HrefAttributeName);
                var existingAttribute = output.Attributes[index];
                output.Attributes[index] = new TagHelperAttribute(
                    existingAttribute.Name,
                    href,
                    existingAttribute.ValueStyle);
            }
        }

        var builder = output.PostElement;
        builder.Clear();

        if (mode == Mode.GlobbedHref || mode == Mode.Fallback && !string.IsNullOrEmpty(HrefInclude))
        {
            BuildGlobbedLinkTags(output.Attributes, builder);
            if (string.IsNullOrEmpty(Href))
            {
                // Only HrefInclude is specified. Don't render the original tag.
                output.TagName = null;
                output.Content.SetHtmlContent(HtmlString.Empty);
            }
        }

        if (mode == Mode.Fallback && HasStyleSheetLinkType(output.Attributes))
        {
            if (TryResolveUrl(FallbackHref, resolvedUrl: out string resolvedUrl))
            {
                FallbackHref = resolvedUrl;
            }

            BuildFallbackBlock(output.Attributes, builder);
        }
    }

    private void BuildGlobbedLinkTags(TagHelperAttributeList attributes, TagHelperContent builder)
    {
        EnsureGlobbingUrlBuilder();

        // Build a <link /> tag for each matched href.
        var urls = GlobbingUrlBuilder.BuildUrlList(null, HrefInclude, HrefExclude);
        for (var i = 0; i < urls.Count; i++)
        {
            var url = urls[i];

            // "url" values come from bound attributes and globbing. Must always be non-null.
            Debug.Assert(url != null);

            if (string.Equals(Href, url, StringComparison.OrdinalIgnoreCase))
            {
                // Don't build duplicate link tag for the original href url.
                continue;
            }

            BuildLinkTag(url, attributes, builder);
        }
    }

    private void BuildFallbackBlock(TagHelperAttributeList attributes, TagHelperContent builder)
    {
        EnsureGlobbingUrlBuilder();
        var fallbackHrefs = GlobbingUrlBuilder.BuildUrlList(
            FallbackHref,
            FallbackHrefInclude,
            FallbackHrefExclude);

        if (fallbackHrefs.Count == 0)
        {
            return;
        }

        builder.AppendHtml(HtmlString.NewLine);

        // Build the <meta /> tag that's used to test for the presence of the stylesheet
        builder
            .AppendHtml("<meta name=\"x-stylesheet-fallback-test\" content=\"\" class=\"")
            .Append(FallbackTestClass)
            .AppendHtml("\" />");

        // Build the <script /> tag that checks the effective style of <meta /> tag above and renders the extra
        // <link /> tag to load the fallback stylesheet if the test CSS property value is found to be false,
        // indicating that the primary stylesheet failed to load.
        // GetEmbeddedJavaScript returns JavaScript to which we add '"{0}","{1}",{2});'
        builder
            .AppendHtml("<script>")
            .AppendHtml(JavaScriptResources.GetEmbeddedJavaScript(FallbackJavaScriptResourceName))
            .AppendHtml("\"")
            .AppendHtml(JavaScriptEncoder.Encode(FallbackTestProperty))
            .AppendHtml("\",\"")
            .AppendHtml(JavaScriptEncoder.Encode(FallbackTestValue))
            .AppendHtml("\",");

        AppendFallbackHrefs(builder, fallbackHrefs);

        builder.AppendHtml(", \"");

        // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
        var attributesCount = attributes.Count;
        for (var i = 0; i < attributesCount; i++)
        {
            var attribute = attributes[i];
            if (string.Equals(attribute.Name, HrefAttributeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (SuppressFallbackIntegrity && string.Equals(attribute.Name, IntegrityAttributeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            attribute.WriteTo(StringWriter, HtmlEncoder);
            StringWriter.Write(' ');
        }

        var stringBuilder = StringWriter.GetStringBuilder();
        var scriptTags = stringBuilder.ToString();
        stringBuilder.Clear();
        var encodedScriptTags = JavaScriptEncoder.Encode(scriptTags);
        builder.AppendHtml(encodedScriptTags);

        builder.AppendHtml("\");</script>");
    }

    private bool HasStyleSheetLinkType(TagHelperAttributeList attributes)
    {
        if (!attributes.TryGetAttribute(RelAttributeName, out var relAttribute) ||
            relAttribute.Value == null)
        {
            return false;
        }

        var attributeValue = relAttribute.Value;
        var stringValue = attributeValue as string;
        if (attributeValue is IHtmlContent contentValue)
        {
            contentValue.WriteTo(StringWriter, HtmlEncoder);
            stringValue = StringWriter.ToString();

            // Reset writer
            StringWriter.GetStringBuilder().Clear();
        }
        else if (stringValue == null)
        {
            stringValue = Convert.ToString(attributeValue, CultureInfo.InvariantCulture);
        }

        var hasRelStylesheet = string.Equals("stylesheet", stringValue, StringComparison.Ordinal);

        return hasRelStylesheet;
    }

    private void AppendFallbackHrefs(TagHelperContent builder, IReadOnlyList<string> fallbackHrefs)
    {
        builder.AppendHtml("[");
        var firstAdded = false;

        // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
        var fallbackHrefsCount = fallbackHrefs.Count;
        for (var i = 0; i < fallbackHrefsCount; i++)
        {
            if (firstAdded)
            {
                builder.AppendHtml(",\"");
            }
            else
            {
                builder.AppendHtml("\"");
                firstAdded = true;
            }

            // fallbackHrefs come from bound attributes (a C# context) and globbing. Must always be non-null.
            Debug.Assert(fallbackHrefs[i] != null);

            var valueToWrite = fallbackHrefs[i];
            if (AppendVersion == true)
            {
                valueToWrite = GetVersionedResourceUrl(fallbackHrefs[i]);
            }

            // Must HTML-encode the href attribute value to ensure the written <link/> element is valid. Must also
            // JavaScript-encode that value to ensure the doc.write() statement is valid.
            valueToWrite = HtmlEncoder.Encode(valueToWrite);
            valueToWrite = JavaScriptEncoder.Encode(valueToWrite);

            builder.AppendHtml(valueToWrite);
            builder.AppendHtml("\"");
        }
        builder.AppendHtml("]");
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
        if (FileVersionProvider == null)
        {
            FileVersionProvider = ViewContext.HttpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
        }
    }

    private void BuildLinkTag(string href, TagHelperAttributeList attributes, TagHelperContent builder)
    {
        builder.AppendHtml("<link ");

        var addHref = true;

        // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
        var attributesCount = attributes.Count;
        for (var i = 0; i < attributesCount; i++)
        {
            var attribute = attributes[i];

            if (string.Equals(attribute.Name, HrefAttributeName, StringComparison.OrdinalIgnoreCase))
            {
                addHref = false;

                AppendVersionedHref(attribute.Name, href, builder);
            }
            else
            {
                attribute.CopyTo(builder);
                builder.AppendHtml(" ");
            }
        }

        if (addHref)
        {
            AppendVersionedHref(HrefAttributeName, href, builder);
        }

        builder.AppendHtml("/>");
    }

    private void AppendVersionedHref(string hrefName, string hrefValue, TagHelperContent builder)
    {
        hrefValue = GetVersionedResourceUrl(hrefValue);
        builder
            .AppendHtml(hrefName)
            .AppendHtml("=\"")
            .Append(hrefValue)
            .AppendHtml("\" ");
    }

    private string GetVersionedResourceUrl(string url)
    {
        if (AppendVersion == true)
        {
            var pathBase = ViewContext.HttpContext.Request.PathBase;

            if (ResourceCollectionUtilities.TryResolveFromAssetCollection(ViewContext, url, out var resolvedUrl))
            {
                url = resolvedUrl;
                return url;
            }

            if (url != null)
            {
                url = FileVersionProvider.AddFileVersionToPath(pathBase, url);
            }
        }

        return url;
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
