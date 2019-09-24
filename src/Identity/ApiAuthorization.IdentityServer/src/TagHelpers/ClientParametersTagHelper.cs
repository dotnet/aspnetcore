// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
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
}
