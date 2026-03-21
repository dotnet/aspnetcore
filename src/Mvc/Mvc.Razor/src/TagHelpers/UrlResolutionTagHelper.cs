// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting elements containing attributes with URL expected values.
/// </summary>
/// <remarks>Resolves URLs starting with '~/' (relative to the application's 'webroot' setting) that are not
/// targeted by other <see cref="ITagHelper"/>s. Runs prior to other <see cref="ITagHelper"/>s to ensure
/// application-relative URLs are resolved.</remarks>
[HtmlTargetElement("*", Attributes = "[itemid^='~/']")]
[HtmlTargetElement("a", Attributes = "[href^='~/']")]
[HtmlTargetElement("applet", Attributes = "[archive^='~/']")]
[HtmlTargetElement("area", Attributes = "[href^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("audio", Attributes = "[src^='~/']")]
[HtmlTargetElement("base", Attributes = "[href^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("blockquote", Attributes = "[cite^='~/']")]
[HtmlTargetElement("button", Attributes = "[formaction^='~/']")]
[HtmlTargetElement("del", Attributes = "[cite^='~/']")]
[HtmlTargetElement("embed", Attributes = "[src^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("form", Attributes = "[action^='~/']")]
[HtmlTargetElement("html", Attributes = "[manifest^='~/']")]
[HtmlTargetElement("iframe", Attributes = "[src^='~/']")]
[HtmlTargetElement("img", Attributes = "[src^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("img", Attributes = "[srcset^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = "[src^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("input", Attributes = "[formaction^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("ins", Attributes = "[cite^='~/']")]
[HtmlTargetElement("link", Attributes = "[href^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("menuitem", Attributes = "[icon^='~/']")]
[HtmlTargetElement("object", Attributes = "[archive^='~/']")]
[HtmlTargetElement("object", Attributes = "[data^='~/']")]
[HtmlTargetElement("q", Attributes = "[cite^='~/']")]
[HtmlTargetElement("script", Attributes = "[src^='~/']")]
[HtmlTargetElement("source", Attributes = "[src^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("source", Attributes = "[srcset^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("track", Attributes = "[src^='~/']", TagStructure = TagStructure.WithoutEndTag)]
[HtmlTargetElement("video", Attributes = "[src^='~/']")]
[HtmlTargetElement("video", Attributes = "[poster^='~/']")]
public class UrlResolutionTagHelper : TagHelper
{
    // Valid whitespace characters defined by the HTML5 spec.
    private static readonly SearchValues<char> ValidAttributeWhitespaceChars = SearchValues.Create("\t\n\u000C\r ");

    private static readonly Dictionary<string, string[]> ElementAttributeLookups =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "a", new[] { "href" } },
            { "applet", new[] { "archive" } },
            { "area", new[] { "href" } },
            { "audio", new[] { "src" } },
            { "base", new[] { "href" } },
            { "blockquote", new[] { "cite" } },
            { "button", new[] { "formaction" } },
            { "del", new[] { "cite" } },
            { "embed", new[] { "src" } },
            { "form", new[] { "action" } },
            { "html", new[] { "manifest" } },
            { "iframe", new[] { "src" } },
            { "img", new[] { "src", "srcset" } },
            { "input", new[] { "src", "formaction" } },
            { "ins", new[] { "cite" } },
            { "link", new[] { "href" } },
            { "menuitem", new[] { "icon" } },
            { "object", new[] { "archive", "data" } },
            { "q", new[] { "cite" } },
            { "script", new[] { "src" } },
            { "source", new[] { "src", "srcset" } },
            { "track", new[] { "src" } },
            { "video", new[] { "poster", "src" } },
        };

    /// <summary>
    /// Creates a new <see cref="UrlResolutionTagHelper"/>.
    /// </summary>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    public UrlResolutionTagHelper(IUrlHelperFactory urlHelperFactory, HtmlEncoder htmlEncoder)
    {
        UrlHelperFactory = urlHelperFactory;
        HtmlEncoder = htmlEncoder;
    }

    /// <inheritdoc />
    public override int Order => -1000 - 999;

    /// <summary>
    /// The <see cref="IUrlHelperFactory"/>.
    /// </summary>
    protected IUrlHelperFactory UrlHelperFactory { get; }

    /// <summary>
    /// The <see cref="HtmlEncoder"/>.
    /// </summary>
    protected HtmlEncoder HtmlEncoder { get; }

    /// <summary>
    /// The <see cref="ViewContext"/>.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (output.TagName == null)
        {
            return;
        }

        if (ElementAttributeLookups.TryGetValue(output.TagName, out var attributeNames))
        {
            for (var i = 0; i < attributeNames.Length; i++)
            {
                ProcessUrlAttribute(attributeNames[i], output);
            }
        }

        // itemid can be present on any HTML element.
        ProcessUrlAttribute("itemid", output);
    }

    /// <summary>
    /// Resolves and updates URL values starting with '~/' (relative to the application's 'webroot' setting) for
    /// <paramref name="output"/>'s <see cref="TagHelperOutput.Attributes"/> whose
    /// <see cref="TagHelperAttribute.Name"/> is <paramref name="attributeName"/>.
    /// </summary>
    /// <param name="attributeName">The attribute name used to lookup values to resolve.</param>
    /// <param name="output">The <see cref="TagHelperOutput"/>.</param>
    protected void ProcessUrlAttribute(string attributeName, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(output);

        var attributes = output.Attributes;
        // Read interface .Count once rather than per iteration
        var attributesCount = attributes.Count;
        for (var i = 0; i < attributesCount; i++)
        {
            var attribute = attributes[i];
            if (!string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (attribute.Value is string stringValue)
            {
                if (TryResolveUrl(stringValue, resolvedUrl: out string? resolvedUrl))
                {
                    attributes[i] = new TagHelperAttribute(
                        attribute.Name,
                        resolvedUrl,
                        attribute.ValueStyle);
                }
            }
            else
            {
                if (attribute.Value is IHtmlContent htmlContent)
                {
                    var htmlString = htmlContent as HtmlString;
                    if (htmlString != null)
                    {
                        // No need for a StringWriter in this case.
                        stringValue = htmlString.ToString();
                    }
                    else
                    {
                        using (var writer = new StringWriter())
                        {
                            htmlContent.WriteTo(writer, HtmlEncoder);
                            stringValue = writer.ToString();
                        }
                    }

                    if (TryResolveUrl(stringValue, resolvedUrl: out IHtmlContent? resolvedUrl))
                    {
                        attributes[i] = new TagHelperAttribute(
                            attribute.Name,
                            resolvedUrl,
                            attribute.ValueStyle);
                    }
                    else if (htmlString == null)
                    {
                        // Not a ~/ URL. Just avoid re-encoding the attribute value later.
                        attributes[i] = new TagHelperAttribute(
                            attribute.Name,
                            new HtmlString(stringValue),
                            attribute.ValueStyle);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tries to resolve the given <paramref name="url"/> value relative to the application's 'webroot' setting.
    /// </summary>
    /// <param name="url">The URL to resolve.</param>
    /// <param name="resolvedUrl">Absolute URL beginning with the application's virtual root. <c>null</c> if
    /// <paramref name="url"/> could not be resolved.</param>
    /// <returns><c>true</c> if the <paramref name="url"/> could be resolved; <c>false</c> otherwise.</returns>
    protected bool TryResolveUrl([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string url, out string? resolvedUrl)
    {
        resolvedUrl = null;
        if (!TryCreateTrimmedString(url, out var trimmedUrl))
        {
            return false;
        }

        trimmedUrl = GetVersionedResourceUrl(trimmedUrl);

        var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
        resolvedUrl = urlHelper.Content(trimmedUrl);

        return true;
    }

    /// <summary>
    /// Tries to resolve the given <paramref name="url"/> value relative to the application's 'webroot' setting.
    /// </summary>
    /// <param name="url">The URL to resolve.</param>
    /// <param name="resolvedUrl">
    /// Absolute URL beginning with the application's virtual root. <c>null</c> if <paramref name="url"/> could
    /// not be resolved.
    /// </param>
    /// <returns><c>true</c> if the <paramref name="url"/> could be resolved; <c>false</c> otherwise.</returns>
    protected bool TryResolveUrl([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string url, [NotNullWhen(true)] out IHtmlContent? resolvedUrl)
    {
        resolvedUrl = null;
        if (!TryCreateTrimmedString(url, out var trimmedUrl))
        {
            return false;
        }

        trimmedUrl = GetVersionedResourceUrl(trimmedUrl);

        var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
        var appRelativeUrl = urlHelper.Content(trimmedUrl);
        var postTildeSlashUrlValue = trimmedUrl.Substring(2);

        if (!appRelativeUrl.EndsWith(postTildeSlashUrlValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                Resources.FormatCouldNotResolveApplicationRelativeUrl_TagHelper(
                    url,
                    nameof(IUrlHelper),
                    nameof(IUrlHelper.Content),
                    "removeTagHelper",
                    typeof(UrlResolutionTagHelper).FullName,
                    typeof(UrlResolutionTagHelper).Assembly.GetName().Name));
        }

        resolvedUrl = new EncodeFirstSegmentContent(
            appRelativeUrl,
            appRelativeUrl.Length - postTildeSlashUrlValue.Length,
            postTildeSlashUrlValue);

        return true;
    }

    private static bool TryCreateTrimmedString(string input, [NotNullWhen(true)] out string? trimmed)
    {
        trimmed = null;
        if (input == null)
        {
            return false;
        }

        var url = input.AsSpan();
        var start = url.IndexOfAnyExcept(ValidAttributeWhitespaceChars);
        if (start < 0)
        {
            return false;
        }

        // Url without leading whitespace.
        url = url.Slice(start);

        // Before doing more work, ensure that the URL we're looking at is app-relative.
        if (!url.StartsWith("~/"))
        {
            return false;
        }

        var remainingLength = url.LastIndexOfAnyExcept(ValidAttributeWhitespaceChars) + 1;

        // Substring returns same string if start == 0 && len == Length
        trimmed = input.Substring(start, remainingLength);
        return true;
    }

    private string GetVersionedResourceUrl(string value)
    {
        var assetCollection = GetAssetCollection();
        if (assetCollection != null)
        {
            var (key, remainder) = ExtractKeyAndRest(value);

            var src = assetCollection[key];
            if (!string.Equals(src, key, StringComparison.Ordinal))
            {
                return $"~/{src}{value[remainder..]}";
            }
        }

        return value;

        static (string key, int rest) ExtractKeyAndRest(string value)
        {
            var lastNonWhitespaceChar = value.AsSpan().TrimEnd().LastIndexOfAnyExcept(ValidAttributeWhitespaceChars);
            var keyEnd = lastNonWhitespaceChar > -1 ? lastNonWhitespaceChar + 1 : value.Length;
            var key = value.AsSpan();
            if (key.StartsWith("~/", StringComparison.Ordinal))
            {
                key = value.AsSpan()[2..keyEnd].Trim();
            }

            return (key.ToString(), keyEnd);
        }
    }

    private ResourceAssetCollection? GetAssetCollection()
    {
        return ViewContext.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ResourceAssetCollection>();
    }

    private sealed class EncodeFirstSegmentContent : IHtmlContent
    {
        private readonly string _firstSegment;
        private readonly int _firstSegmentLength;
        private readonly string _secondSegment;

        public EncodeFirstSegmentContent(string firstSegment, int firstSegmentLength, string secondSegment)
        {
            _firstSegment = firstSegment;
            _firstSegmentLength = firstSegmentLength;
            _secondSegment = secondSegment;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            encoder.Encode(writer, _firstSegment, 0, _firstSegmentLength);
            writer.Write(_secondSegment);
        }
    }
}
