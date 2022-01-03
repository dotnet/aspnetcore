// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// Implements IRazorPage so that RazorPageBase-derived classes don't get activated twice.
/// The page gets activated before handler methods run, but the RazorView will also activate
/// each page.
/// </summary>
public class RazorPageAdapter : IRazorPage, IModelTypeProvider
{
    private readonly RazorPageBase _page;
    private readonly Type _modelType;

    /// <summary>
    /// Instantiates a new instance of <see cref="RazorPageAdapter"/>.
    /// </summary>
    /// <param name="page">The <see cref="RazorPageBase"/>.</param>
    /// <param name="modelType">The model type.</param>
    public RazorPageAdapter(RazorPageBase page, Type modelType)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _modelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
    }

    /// <inheritdoc/>
    public ViewContext ViewContext
    {
        get { return _page.ViewContext; }
        set { _page.ViewContext = value; }
    }

    /// <inheritdoc/>
    public IHtmlContent? BodyContent
    {
        get { return _page.BodyContent; }
        set { _page.BodyContent = value; }
    }

    /// <inheritdoc/>
    public bool IsLayoutBeingRendered
    {
        get { return _page.IsLayoutBeingRendered; }
        set { _page.IsLayoutBeingRendered = value; }
    }

    /// <inheritdoc/>
    public string Path
    {
        get { return _page.Path; }
        set { _page.Path = value; }
    }

    /// <inheritdoc/>
    public string? Layout
    {
        get { return _page.Layout; }
        set { _page.Layout = value; }
    }

    /// <inheritdoc/>
    public IDictionary<string, RenderAsyncDelegate> PreviousSectionWriters
    {
        get { return _page.PreviousSectionWriters; }
        set { _page.PreviousSectionWriters = value; }
    }

    /// <inheritdoc/>
    public IDictionary<string, RenderAsyncDelegate> SectionWriters => _page.SectionWriters;

    /// <inheritdoc/>
    public void EnsureRenderedBodyOrSections()
    {
        _page.EnsureRenderedBodyOrSections();
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        return _page.ExecuteAsync();
    }

    Type IModelTypeProvider.GetModelType() => _modelType;
}
