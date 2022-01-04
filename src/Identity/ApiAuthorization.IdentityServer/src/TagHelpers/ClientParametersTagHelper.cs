// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

/// <summary>
/// A tag helper for generating client parameters for a given oauth/openid client as data attributes.
/// </summary>
[HtmlTargetElement("*", Attributes = "[asp-apiauth-parameters]")]
public class ClientParametersTagHelper : TagHelper
{
    private readonly IClientRequestParametersProvider _clientRequestParametersProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="ClientParametersTagHelper"/>.
    /// </summary>
    /// <param name="clientRequestParametersProvider">The <see cref="IClientRequestParametersProvider"/>.</param>
    public ClientParametersTagHelper(IClientRequestParametersProvider clientRequestParametersProvider)
    {
        _clientRequestParametersProvider = clientRequestParametersProvider;
    }

    /// <summary>
    /// Gets or sets the client id.
    /// </summary>
    [HtmlAttributeName("asp-apiauth-parameters")]
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the ViewContext.
    /// </summary>
    [ViewContext]
    public ViewContext ViewContext { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var parameters = _clientRequestParametersProvider.GetClientParameters(ViewContext.HttpContext, ClientId);
        if (parameters == null)
        {
            throw new InvalidOperationException($"Parameters for client '{ClientId}' not found.");
        }

        foreach (var parameter in parameters)
        {
            output.Attributes.Add("data-" + parameter.Key, parameter.Value);
        }
    }
}
