// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A <see cref="IViewComponentResult"/> that renders a partial view when executed.
/// </summary>
public class ViewViewComponentResult : IViewComponentResult
{
    // {0} is the component name, {1} is the view name.
    private const string ViewPathFormat = "Components/{0}/{1}";
    private const string DefaultViewName = "Default";

    private DiagnosticListener? _diagnosticListener;

    /// <summary>
    /// Gets or sets the view name.
    /// </summary>
    public string? ViewName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ViewDataDictionary"/>.
    /// </summary>
    public ViewDataDictionary? ViewData { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ITempDataDictionary"/> instance.
    /// </summary>
    public ITempDataDictionary TempData { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="ViewEngine"/>.
    /// </summary>
    public IViewEngine? ViewEngine { get; set; }

    /// <summary>
    /// Locates and renders a view specified by <see cref="ViewName"/>. If <see cref="ViewName"/> is <c>null</c>,
    /// then the view name searched for is<c>&quot;Default&quot;</c>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
    /// <remarks>
    /// This method synchronously calls and blocks on <see cref="ExecuteAsync(ViewComponentContext)"/>.
    /// </remarks>
    public void Execute(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var task = ExecuteAsync(context);
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Locates and renders a view specified by <see cref="ViewName"/>. If <see cref="ViewName"/> is <c>null</c>,
    /// then the view name searched for is<c>&quot;Default&quot;</c>.
    /// </summary>
    /// <param name="context">The <see cref="ViewComponentContext"/> for the current component execution.</param>
    /// <returns>A <see cref="Task"/> which will complete when view rendering is completed.</returns>
    public async Task ExecuteAsync(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var viewEngine = ViewEngine ?? ResolveViewEngine(context);
        var viewContext = context.ViewContext;
        var isNullOrEmptyViewName = string.IsNullOrEmpty(ViewName);

        ViewEngineResult? result = null;
        IEnumerable<string>? originalLocations = null;
        if (!isNullOrEmptyViewName)
        {
            // If view name was passed in is already a path, the view engine will handle this.
            result = viewEngine.GetView(viewContext.ExecutingFilePath, ViewName!, isMainPage: false);
            originalLocations = result.SearchedLocations;
        }

        if (result == null || !result.Success)
        {
            // This will produce a string like:
            //
            //  Components/Cart/Default
            //
            // The view engine will combine this with other path info to search paths like:
            //
            //  Views/Shared/Components/Cart/Default.cshtml
            //  Views/Home/Components/Cart/Default.cshtml
            //  Areas/Blog/Views/Shared/Components/Cart/Default.cshtml
            //
            // This supports a controller or area providing an override for component views.
            var viewName = isNullOrEmptyViewName ? DefaultViewName : ViewName;
            var qualifiedViewName = string.Format(
                CultureInfo.InvariantCulture,
                ViewPathFormat,
                context.ViewComponentDescriptor.ShortName,
                viewName);

            result = viewEngine.FindView(viewContext, qualifiedViewName, isMainPage: false);
        }

        var view = result.EnsureSuccessful(originalLocations).View!;
        using (view as IDisposable)
        {
            if (_diagnosticListener == null)
            {
                _diagnosticListener = viewContext.HttpContext.RequestServices.GetRequiredService<DiagnosticListener>();
            }

            _diagnosticListener.ViewComponentBeforeViewExecute(context, view);

            var childViewContext = new ViewContext(
                viewContext,
                view,
                ViewData ?? context.ViewData,
                context.Writer);
            await view.RenderAsync(childViewContext);

            _diagnosticListener.ViewComponentAfterViewExecute(context, view);
        }
    }

    private static IViewEngine ResolveViewEngine(ViewComponentContext context)
    {
        return context.ViewContext.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
    }
}
