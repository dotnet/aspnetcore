// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class RenderFragmentExtensions
{
    public static async Task<string> ToHtmlAsync(
        this RenderFragment fragment,
        IServiceProvider services,
        ILoggerFactory loggerFactory)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(FragmentRenderer.ChildContent)] = fragment
            });
            var output = await renderer.RenderComponentAsync<FragmentRenderer>(parameters);

            return output.ToHtmlString();
        });
    }

    private sealed class FragmentRenderer : ComponentBase
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}
