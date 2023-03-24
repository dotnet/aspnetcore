// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.HtmlRendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A subclass of <see cref="HtmlRendererCore" /> that deals with concerns specific to prerendering
/// on an endpoint, e.g., streaming rendering or dispatching form post events.
/// </summary>
internal class EndpointHtmlRenderer : HtmlRendererCore
{
    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
    }

    public async Task<HtmlComponent> RenderComponentAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        ParameterView parameters)
    {
        var content = BeginRenderingComponent(componentType, parameters);
        await content.WaitForQuiescenceAsync();
        return content;
    }
}
