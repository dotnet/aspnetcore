// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Internal component type that acts as a root component when executing a RazorComponentResult. It takes
/// care of rendering the component inside its hierarchy of layouts (via LayoutView) as well as converting
/// any information we want from RazorComponentResult into component parameters. We could also use this to
/// supply other data from the original HttpContext as component parameters, e.g., for model binding.
///
/// It happens to be almost the same as RouteView except it doesn't supply any query parameters. We can
/// resolve that at the same time we implement support for form posts.
/// </summary>
internal class RazorComponentResultHost : IComponent
{
    private RenderHandle _renderHandle;

    public RazorComponentResult RazorComponentResult { get; private set; }

    public void Attach(RenderHandle renderHandle)
        => _renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var kvp in parameters)
        {
            switch (kvp.Name)
            {
                case nameof(RazorComponentResult):
                    RazorComponentResult = (RazorComponentResult)kvp.Value;
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter '{kvp.Name}'");
            }
        }

        if (RazorComponentResult is null)
        {
            throw new InvalidOperationException($"Failed to supply nonnull value for required parameter '{nameof(RazorComponentResult)}'");
        }

        _renderHandle.Render(BuildRenderTree);
        return Task.CompletedTask;
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        var pageLayoutType = RazorComponentResult.ComponentType.GetCustomAttribute<LayoutAttribute>()?.LayoutType;

        builder.OpenComponent<LayoutView>(0);
        builder.AddComponentParameter(1, nameof(LayoutView.Layout), pageLayoutType);
        builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), (RenderFragment)RenderPageWithParameters);
        builder.CloseComponent();
    }

    private void RenderPageWithParameters(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, RazorComponentResult.ComponentType);

        if (RazorComponentResult.Parameters is not null)
        {
            var dict = PropertyHelper.ObjectToDictionary(RazorComponentResult.Parameters);
            foreach (var kvp in dict)
            {
                builder.AddComponentParameter(1, kvp.Key, kvp.Value);
            }
        }

        builder.CloseComponent();
    }
}
