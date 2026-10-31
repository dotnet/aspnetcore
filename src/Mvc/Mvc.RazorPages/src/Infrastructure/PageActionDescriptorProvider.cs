// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// A <see cref="IActionDescriptorProvider"/> for PageActions
/// </summary>
public class PageActionDescriptorProvider : IActionDescriptorProvider
{
    private readonly IPageRouteModelProvider[] _routeModelProviders;
    private readonly MvcOptions _mvcOptions;
    private readonly IPageRouteModelConvention[] _conventions;

    /// <summary>
    /// Instantiates a new instance of <see cref="PageActionDescriptorProvider"/>.
    /// </summary>
    /// <param name="pageRouteModelProviders">The <see cref="IPageRouteModelProvider"/>s to use.</param>
    /// <param name="mvcOptionsAccessor">The <see cref="MvcOptions"/>.</param>
    /// <param name="pagesOptionsAccessor">The <see cref="RazorPagesOptions"/>.</param>
    public PageActionDescriptorProvider(
        IEnumerable<IPageRouteModelProvider> pageRouteModelProviders,
        IOptions<MvcOptions> mvcOptionsAccessor,
        IOptions<RazorPagesOptions> pagesOptionsAccessor)
    {
        _routeModelProviders = pageRouteModelProviders.OrderBy(p => p.Order).ToArray();
        _mvcOptions = mvcOptionsAccessor.Value;

        _conventions = pagesOptionsAccessor.Value.Conventions
            .OfType<IPageRouteModelConvention>()
            .ToArray();
    }

    /// <inheritdoc/>
    public int Order { get; set; } = -900; // Run after the default MVC provider, but before others.

    /// <inheritdoc/>
    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
        var pageRouteModels = BuildModel();

        for (var i = 0; i < pageRouteModels.Count; i++)
        {
            AddActionDescriptors(context.Results, pageRouteModels[i]);
        }
    }

    /// <summary>
    /// Build the model.
    /// </summary>
    /// <returns>The list of <see cref="PageRouteModel"/>.</returns>
    protected IList<PageRouteModel> BuildModel()
    {
        var context = new PageRouteModelProviderContext();

        for (var i = 0; i < _routeModelProviders.Length; i++)
        {
            _routeModelProviders[i].OnProvidersExecuting(context);
        }

        for (var i = _routeModelProviders.Length - 1; i >= 0; i--)
        {
            _routeModelProviders[i].OnProvidersExecuted(context);
        }

        return context.RouteModels;
    }

    /// <inheritdoc/>
    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
    }

    private void AddActionDescriptors(IList<ActionDescriptor> actions, PageRouteModel model)
    {
        for (var i = 0; i < _conventions.Length; i++)
        {
            _conventions[i].Apply(model);
        }

        foreach (var selector in model.Selectors)
        {
            var descriptor = new PageActionDescriptor
            {
                ActionConstraints = selector.ActionConstraints.ToList(),
                AreaName = model.AreaName,
                AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = selector.AttributeRouteModel!.Name,
                    Order = selector.AttributeRouteModel.Order ?? 0,
                    Template = TransformPageRoute(model, selector),
                    SuppressLinkGeneration = selector.AttributeRouteModel.SuppressLinkGeneration,
                    SuppressPathMatching = selector.AttributeRouteModel.SuppressPathMatching,
                },
                DisplayName = $"Page: {model.ViewEnginePath}",
                EndpointMetadata = selector.EndpointMetadata.ToList(),
                FilterDescriptors = Array.Empty<FilterDescriptor>(),
                Properties = new Dictionary<object, object?>(model.Properties),
                RelativePath = model.RelativePath,
                ViewEnginePath = model.ViewEnginePath,
            };

            foreach (var kvp in model.RouteValues)
            {
                if (!descriptor.RouteValues.ContainsKey(kvp.Key))
                {
                    descriptor.RouteValues.Add(kvp.Key, kvp.Value);
                }
            }

            if (!descriptor.RouteValues.ContainsKey("page"))
            {
                descriptor.RouteValues.Add("page", model.ViewEnginePath);
            }

            actions.Add(descriptor);
        }
    }

    private static string? TransformPageRoute(PageRouteModel model, SelectorModel selectorModel)
    {
        // Transformer not set on page route
        if (model.RouteParameterTransformer == null)
        {
            return selectorModel.AttributeRouteModel!.Template;
        }

        var pageRouteMetadata = selectorModel.EndpointMetadata.OfType<PageRouteMetadata>().SingleOrDefault();
        if (pageRouteMetadata == null)
        {
            // Selector does not have expected metadata
            // This selector was likely configured by AddPageRouteModelConvention
            // Use the existing explicitly configured template
            return selectorModel.AttributeRouteModel!.Template;
        }

        var segments = (string?[])pageRouteMetadata.PageRoute.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = model.RouteParameterTransformer.TransformOutbound(segments[i]);
        }

        var transformedPageRoute = string.Join("/", segments);

        // Combine transformed page route with template
        return AttributeRouteModel.CombineTemplates(transformedPageRoute, pageRouteMetadata.RouteTemplate);
    }
}
