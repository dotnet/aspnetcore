// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;script&gt; elements that supports fallback src paths.
/// </summary>
/// <remarks>
/// The tag helper won't process for cases with just the 'src' attribute.
/// </remarks>
[HtmlTargetElement("script", Attributes = SrcIncludeAttributeName)]
[HtmlTargetElement("script", Attributes = SrcExcludeAttributeName)]
[HtmlTargetElement("script", Attributes = FallbackSrcAttributeName)]
[HtmlTargetElement("script", Attributes = FallbackSrcIncludeAttributeName)]
[HtmlTargetElement("script", Attributes = FallbackSrcExcludeAttributeName)]
[HtmlTargetElement("script", Attributes = FallbackTestExpressionAttributeName)]
[HtmlTargetElement("script", Attributes = AppendVersionAttributeName)]
[HtmlTargetElement("script", Attributes = TypeAttributeName)]
[HtmlTargetElement("script", Attributes = ImportMapAttributeName)]
public class ScriptTagHelper : UrlResolutionTagHelper
{
    private const string SrcIncludeAttributeName = "asp-src-include";
    private const string SrcExcludeAttributeName = "asp-src-exclude";
    private const string FallbackSrcAttributeName = "asp-fallback-src";
    private const string FallbackSrcIncludeAttributeName = "asp-fallback-src-include";
    private const string SuppressFallbackIntegrityAttributeName = "asp-suppress-fallback-integrity";
    private const string FallbackSrcExcludeAttributeName = "asp-fallback-src-exclude";
    private const string FallbackTestExpressionAttributeName = "asp-fallback-test";
    private const string SrcAttributeName = "src";
    private const string IntegrityAttributeName = "integrity";
    private const string AppendVersionAttributeName = "asp-append-version";
    private const string TypeAttributeName = "type";
    private const string ImportMapAttributeName = "asp-importmap";

    private static readonly Func<Mode, Mode, int> Compare = (a, b) => a - b;
    private StringWriter _stringWriter;

    private static readonly ModeAttributes<Mode>[] ModeDetails = new[] {
            // Regular src with file version alone
            new ModeAttributes<Mode>(Mode.AppendVersion, new[] { AppendVersionAttributeName }),
            // Globbed src (include only)
            new ModeAttributes<Mode>(Mode.GlobbedSrc, new [] { SrcIncludeAttributeName }),
            // Globbed src (include & exclude)
            new ModeAttributes<Mode>(Mode.GlobbedSrc, new [] { SrcIncludeAttributeName, SrcExcludeAttributeName }),
            // Fallback with static src
            new ModeAttributes<Mode>(Mode.Fallback,
                new[]
                {
                    FallbackSrcAttributeName,
                    FallbackTestExpressionAttributeName
                }),
            // Fallback with globbed src (include only)
            new ModeAttributes<Mode>(
                Mode.Fallback,
                new[]
                {
                    FallbackSrcIncludeAttributeName,
                    FallbackTestExpressionAttributeName
                }),
            // Fallback with globbed src (include & exclude)
            new ModeAttributes<Mode>(
                Mode.Fallback,
                new[]
                {
                    FallbackSrcIncludeAttributeName,
                    FallbackSrcExcludeAttributeName,
                    FallbackTestExpressionAttributeName
                }),
        };

    /// <summary>
    /// Creates a new <see cref="ScriptTagHelper"/>.
    /// </summary>
    /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
    /// <param name="cacheProvider">The <see cref="TagHelperMemoryCacheProvider"/>.</param>
    /// <param name="fileVersionProvider">The <see cref="IFileVersionProvider"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    /// <param name="javaScriptEncoder">The <see cref="JavaScriptEncoder"/>.</param>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    // Decorated with ActivatorUtilitiesConstructor since we want to influence tag helper activation
    // to use this constructor in the default case.
    public ScriptTagHelper(
        IWebHostEnvironment hostingEnvironment,
        TagHelperMemoryCacheProvider cacheProvider,
        IFileVersionProvider fileVersionProvider,
        HtmlEncoder htmlEncoder,
        JavaScriptEncoder javaScriptEncoder,
        IUrlHelperFactory urlHelperFactory)
        : base(urlHelperFactory, htmlEncoder)
    {
        HostingEnvironment = hostingEnvironment;
        Cache = cacheProvider.Cache;
        JavaScriptEncoder = javaScriptEncoder;

        FileVersionProvider = fileVersionProvider;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Address of the external script to use.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases.
    /// </remarks>
    [HtmlAttributeName(SrcAttributeName)]
    public string Src { get; set; }

    /// <summary>
    /// Type of the script.
    /// </summary>
    [HtmlAttributeName(TypeAttributeName)]
    public string Type { get; set; }

    /// <summary>
    /// A comma separated list of globbed file patterns of JavaScript scripts to load.
    /// The glob patterns are assessed relative to the application's 'webroot' setting.
    /// </summary>
    [HtmlAttributeName(SrcIncludeAttributeName)]
    public string SrcInclude { get; set; }

    /// <summary>
    /// A comma separated list of globbed file patterns of JavaScript scripts to exclude from loading.
    /// The glob patterns are assessed relative to the application's 'webroot' setting.
    /// Must be used in conjunction with <see cref="SrcInclude"/>.
    /// </summary>
    [HtmlAttributeName(SrcExcludeAttributeName)]
    public string SrcExclude { get; set; }

    /// <summary>
    /// The URL of a Script tag to fallback to in the case the primary one fails.
    /// </summary>
    [HtmlAttributeName(FallbackSrcAttributeName)]
    public string FallbackSrc { get; set; }

    /// <summary>
    /// Boolean value that determines if an integrity hash will be compared with <see cref="FallbackSrc"/> value.
    /// </summary>
    [HtmlAttributeName(SuppressFallbackIntegrityAttributeName)]
    public bool SuppressFallbackIntegrity { get; set; }

    /// <summary>
    /// Value indicating if file version should be appended to src urls.
    /// </summary>
    /// <remarks>
    /// A query string "v" with the encoded content of the file is added.
    /// </remarks>
    [HtmlAttributeName(AppendVersionAttributeName)]
    public bool? AppendVersion { get; set; }

    /// <summary>
    /// A comma separated list of globbed file patterns of JavaScript scripts to fallback to in the case the
    /// primary one fails.
    /// The glob patterns are assessed relative to the application's 'webroot' setting.
    /// </summary>
    [HtmlAttributeName(FallbackSrcIncludeAttributeName)]
    public string FallbackSrcInclude { get; set; }

    /// <summary>
    /// A comma separated list of globbed file patterns of JavaScript scripts to exclude from the fallback list, in
    /// the case the primary one fails.
    /// The glob patterns are assessed relative to the application's 'webroot' setting.
    /// Must be used in conjunction with <see cref="FallbackSrcInclude"/>.
    /// </summary>
    [HtmlAttributeName(FallbackSrcExcludeAttributeName)]
    public string FallbackSrcExclude { get; set; }

    /// <summary>
    /// The script method defined in the primary script to use for the fallback test.
    /// </summary>
    [HtmlAttributeName(FallbackTestExpressionAttributeName)]
    public string FallbackTestExpression { get; set; }

    /// <summary>
    /// The <see cref="ImportMapDefinition"/> to use for the document.
    /// </summary>
    /// <remarks>
    /// If this is not set and the type value is "importmap",
    /// the import map will be retrieved by default from the current <see cref="Endpoint.Metadata"/>.
    /// </remarks>
    [HtmlAttributeName(ImportMapAttributeName)]
    public ImportMapDefinition ImportMap { get; set; }

    /// <summary>
    /// Gets the <see cref="IWebHostEnvironment"/> for the application.
    /// </summary>
    protected internal IWebHostEnvironment HostingEnvironment { get; }

    /// <summary>
    /// Gets the <see cref="IMemoryCache"/> used to store globbed urls.
    /// </summary>
    protected internal IMemoryCache Cache { get; private set; }

    internal IFileVersionProvider FileVersionProvider { get; private set; }

    /// <summary>
    /// Gets the <see cref="System.Text.Encodings.Web.JavaScriptEncoder"/> used to encode fallback information.
    /// </summary>
    protected JavaScriptEncoder JavaScriptEncoder { get; }

    /// <summary>
    /// Gets the <see cref="GlobbingUrlBuilder"/> used to populate included and excluded urls.
    /// </summary>
    // Internal for ease of use when testing.
    protected internal GlobbingUrlBuilder GlobbingUrlBuilder { get; set; }

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

        if (string.Equals(Type, "importmap", StringComparison.OrdinalIgnoreCase))
        {
            // This is an importmap script, we'll write out the import map and
            // stop processing.
            var importMap = ImportMap ?? ViewContext.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ImportMapDefinition>();
            if (importMap == null)
            {
                // No importmap found, nothing to do.
                output.SuppressOutput();
                return;
            }

            output.TagName = "script";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("type", "importmap");
            output.Content.SetHtmlContent(importMap.ToString());
            return;
        }

        // Pass through attribute that is also a well-known HTML attribute.
        if (Src != null)
        {
            output.CopyHtmlAttribute(SrcAttributeName, context);
        }

        if (Type != null)
        {
            output.CopyHtmlAttribute(TypeAttributeName, context);
        }

        // If there's no "src" attribute in output.Attributes this will noop.
        ProcessUrlAttribute(SrcAttributeName, output);

        // Retrieve the TagHelperOutput variation of the "src" attribute in case other TagHelpers in the
        // pipeline have touched the value. If the value is already encoded this ScriptTagHelper may
        // not function properly.
        Src = output.Attributes[SrcAttributeName]?.Value as string;

        if (!AttributeMatcher.TryDetermineMode(context, ModeDetails, Compare, out var mode))
        {
            // No attributes matched so we have nothing to do
            return;
        }

        if (AppendVersion == true)
        {
            EnsureFileVersionProvider();
            var versionedSrc = GetVersionedSrc(Src);
            if (Src != null)
            {
                var index = output.Attributes.IndexOfName(SrcAttributeName);
                var existingAttribute = output.Attributes[index];
                output.Attributes[index] = new TagHelperAttribute(
                    existingAttribute.Name,
                    versionedSrc,
                    existingAttribute.ValueStyle);
            }
        }

        var builder = output.PostElement;
        builder.Clear();

        if (mode == Mode.GlobbedSrc || mode == Mode.Fallback && !string.IsNullOrEmpty(SrcInclude))
        {
            BuildGlobbedScriptTags(output.Attributes, builder);
            if (string.IsNullOrEmpty(Src))
            {
                // Only SrcInclude is specified. Don't render the original tag.
                output.TagName = null;
                output.Content.SetContent(string.Empty);
            }
        }

        if (mode == Mode.Fallback)
        {
            if (TryResolveUrl(FallbackSrc, resolvedUrl: out string resolvedUrl))
            {
                FallbackSrc = resolvedUrl;
            }

            BuildFallbackBlock(output.Attributes, builder);
        }
    }

    private void BuildGlobbedScriptTags(
        TagHelperAttributeList attributes,
        TagHelperContent builder)
    {
        EnsureGlobbingUrlBuilder();

        // Build a <script> tag for each matched src as well as the original one in the source file
        var urls = GlobbingUrlBuilder.BuildUrlList(null, SrcInclude, SrcExclude);
        foreach (var url in urls)
        {
            // "url" values come from bound attributes and globbing. Must always be non-null.
            Debug.Assert(url != null);

            if (string.Equals(url, Src, StringComparison.OrdinalIgnoreCase))
            {
                // Don't build duplicate script tag for the original source url.
                continue;
            }

            BuildScriptTag(url, attributes, builder);
        }
    }

    private void BuildFallbackBlock(TagHelperAttributeList attributes, TagHelperContent builder)
    {
        EnsureGlobbingUrlBuilder();

        var fallbackSrcs = GlobbingUrlBuilder.BuildUrlList(FallbackSrc, FallbackSrcInclude, FallbackSrcExclude);
        if (fallbackSrcs.Count > 0)
        {
            // Build the <script> tag that checks the test method and if it fails, renders the extra script.
            builder.AppendHtml(Environment.NewLine)
                   .AppendHtml("<script>(")
                   .AppendHtml(FallbackTestExpression)
                   .AppendHtml("||document.write(\"");

            foreach (var src in fallbackSrcs)
            {
                // Fallback "src" values come from bound attributes and globbing. Must always be non-null.
                Debug.Assert(src != null);

                StringWriter.Write("<script");

                var addSrc = true;

                // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
                var attributesCount = attributes.Count;
                for (var i = 0; i < attributesCount; i++)
                {
                    var attribute = attributes[i];
                    if (!attribute.Name.Equals(SrcAttributeName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (SuppressFallbackIntegrity && string.Equals(IntegrityAttributeName, attribute.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        StringWriter.Write(' ');
                        attribute.WriteTo(StringWriter, HtmlEncoder);
                    }
                    else
                    {
                        addSrc = false;
                        WriteVersionedSrc(attribute.Name, src, attribute.ValueStyle, StringWriter);
                    }
                }

                if (addSrc)
                {
                    WriteVersionedSrc(SrcAttributeName, src, HtmlAttributeValueStyle.DoubleQuotes, StringWriter);
                }

                StringWriter.Write("></script>");
            }

            var stringBuilder = StringWriter.GetStringBuilder();
            var scriptTags = stringBuilder.ToString();
            stringBuilder.Clear();
            var encodedScriptTags = JavaScriptEncoder.Encode(scriptTags);
            builder.AppendHtml(encodedScriptTags);

            builder.AppendHtml("\"));</script>");
        }
    }

    private string GetVersionedSrc(string srcValue)
    {
        if (AppendVersion == true)
        {
            var pathBase = ViewContext.HttpContext.Request.PathBase;
            if (ResourceCollectionUtilities.TryResolveFromAssetCollection(ViewContext, srcValue, out var resolvedUrl))
            {
                srcValue = resolvedUrl;
                return srcValue;
            }

            if (srcValue != null)
            {
                srcValue = FileVersionProvider.AddFileVersionToPath(pathBase, srcValue);
            }
        }

        return srcValue;
    }

    private void AppendVersionedSrc(
        string srcName,
        string srcValue,
        HtmlAttributeValueStyle valueStyle,
        IHtmlContentBuilder builder)
    {
        srcValue = GetVersionedSrc(srcValue);

        builder.AppendHtml(" ");
        var attribute = new TagHelperAttribute(srcName, srcValue, valueStyle);
        attribute.CopyTo(builder);
    }

    private void WriteVersionedSrc(
        string srcName,
        string srcValue,
        HtmlAttributeValueStyle valueStyle,
        TextWriter writer)
    {
        srcValue = GetVersionedSrc(srcValue);

        writer.Write(' ');
        var attribute = new TagHelperAttribute(srcName, srcValue, valueStyle);
        attribute.WriteTo(writer, HtmlEncoder);
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

    private void BuildScriptTag(
        string src,
        TagHelperAttributeList attributes,
        TagHelperContent builder)
    {
        builder.AppendHtml("<script");

        var addSrc = true;

        // Perf: Avoid allocating enumerator and read interface .Count once rather than per iteration
        var attributesCount = attributes.Count;
        for (var i = 0; i < attributesCount; i++)
        {
            var attribute = attributes[i];
            if (!attribute.Name.Equals(SrcAttributeName, StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendHtml(" ");
                attribute.CopyTo(builder);
            }
            else
            {
                addSrc = false;
                AppendVersionedSrc(attribute.Name, src, attribute.ValueStyle, builder);
            }
        }

        if (addSrc)
        {
            AppendVersionedSrc(SrcAttributeName, src, HtmlAttributeValueStyle.DoubleQuotes, builder);
        }

        builder.AppendHtml("></script>");
    }

    private enum Mode
    {
        /// <summary>
        /// Just adding a file version for the generated urls.
        /// </summary>
        AppendVersion = 0,

        /// <summary>
        /// Just performing file globbing search for the src, rendering a separate &lt;script&gt; for each match.
        /// </summary>
        GlobbedSrc = 1,

        /// <summary>
        /// Rendering a fallback block if primary javascript fails to load. Will also do globbing for both the
        /// primary and fallback srcs if the appropriate properties are set.
        /// </summary>
        Fallback = 2
    }
}
