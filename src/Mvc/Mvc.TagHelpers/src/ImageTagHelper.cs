// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;img&gt; elements that supports file versioning.
/// </summary>
/// <remarks>
/// The tag helper won't process for cases with just the 'src' attribute.
/// </remarks>
[HtmlTargetElement(
    "img",
    Attributes = AppendVersionAttributeName + "," + SrcAttributeName,
    TagStructure = TagStructure.WithoutEndTag)]
public class ImageTagHelper : UrlResolutionTagHelper
{
    private const string AppendVersionAttributeName = "asp-append-version";
    private const string SrcAttributeName = "src";

    /// <summary>
    /// Creates a new <see cref="ImageTagHelper"/>.
    /// </summary>
    /// <param name="fileVersionProvider">The <see cref="IFileVersionProvider"/>.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    // Decorated with ActivatorUtilitiesConstructor since we want to influence tag helper activation
    // to use this constructor in the default case.
    [ActivatorUtilitiesConstructor]
    public ImageTagHelper(
        IFileVersionProvider fileVersionProvider,
        HtmlEncoder htmlEncoder,
        IUrlHelperFactory urlHelperFactory)
        : base(urlHelperFactory, htmlEncoder)
    {
        FileVersionProvider = fileVersionProvider;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// Source of the image.
    /// </summary>
    /// <remarks>
    /// Passed through to the generated HTML in all cases.
    /// </remarks>
    [HtmlAttributeName(SrcAttributeName)]
    public string Src { get; set; }

    /// <summary>
    /// Value indicating if file version should be appended to the src urls.
    /// </summary>
    /// <remarks>
    /// If <c>true</c> then a query string "v" with the encoded content of the file is added.
    /// </remarks>
    [HtmlAttributeName(AppendVersionAttributeName)]
    public bool AppendVersion { get; set; }

    internal IFileVersionProvider FileVersionProvider { get; private set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        output.CopyHtmlAttribute(SrcAttributeName, context);
        ProcessUrlAttribute(SrcAttributeName, output);

        if (AppendVersion)
        {
            EnsureFileVersionProvider();

            // Retrieve the TagHelperOutput variation of the "src" attribute in case other TagHelpers in the
            // pipeline have touched the value. If the value is already encoded this ImageTagHelper may
            // not function properly.
            Src = output.Attributes[SrcAttributeName].Value as string;
            var src = GetVersionedResourceUrl(Src);

            output.Attributes.SetAttribute(SrcAttributeName, src);
        }
    }

    private void EnsureFileVersionProvider()
    {
        if (FileVersionProvider == null)
        {
            FileVersionProvider = ViewContext.HttpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
        }
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
}
