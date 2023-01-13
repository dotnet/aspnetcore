// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A model component for routing RazorPages.
/// </summary>
public class PageRouteModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="PageRouteModel"/>.
    /// </summary>
    /// <param name="relativePath">The application relative path of the page.</param>
    /// <param name="viewEnginePath">The path relative to the base path for page discovery.</param>
    public PageRouteModel(string relativePath, string viewEnginePath)
        : this(relativePath, viewEnginePath, areaName: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PageRouteModel"/>.
    /// </summary>
    /// <param name="relativePath">The application relative path of the page.</param>
    /// <param name="viewEnginePath">The path relative to the base path for page discovery.</param>
    /// <param name="areaName">The area name.</param>
    public PageRouteModel(string relativePath, string viewEnginePath, string? areaName)
    {
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        ViewEnginePath = viewEnginePath ?? throw new ArgumentNullException(nameof(viewEnginePath));
        AreaName = areaName;

        Properties = new Dictionary<object, object?>();
        Selectors = new List<SelectorModel>();
        RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A copy constructor for <see cref="PageRouteModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="PageRouteModel"/> to copy from.</param>
    public PageRouteModel(PageRouteModel other)
    {
        ArgumentNullException.ThrowIfNull(other);

        RelativePath = other.RelativePath;
        ViewEnginePath = other.ViewEnginePath;
        AreaName = other.AreaName;
        RouteParameterTransformer = other.RouteParameterTransformer;

        Properties = new Dictionary<object, object?>(other.Properties);
        Selectors = new List<SelectorModel>(other.Selectors.Select(m => new SelectorModel(m)));
        RouteValues = new Dictionary<string, string>(other.RouteValues, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the application root relative path for the page.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the path relative to the base path for page discovery.
    /// <para>
    /// This value is the path of the file without extension, relative to the pages root directory.
    /// e.g. the <see cref="ViewEnginePath"/> for the file /Pages/Catalog/Antiques.cshtml is <c>/Catalog/Antiques</c>
    /// </para>
    /// <para>
    /// In an area, this value is the path of the file without extension, relative to the pages root directory for the specified area.
    /// e.g. the <see cref="ViewEnginePath"/>  for the file Areas/Identity/Pages/Manage/Accounts.cshtml, is <c>/Manage/Accounts</c>.
    /// </para>
    /// </summary>
    public string ViewEnginePath { get; }

    /// <summary>
    /// Gets the area name. Will be <c>null</c> for non-area pages.
    /// </summary>
    public string? AreaName { get; }

    /// <summary>
    /// Stores arbitrary metadata properties associated with the <see cref="PageRouteModel"/>.
    /// </summary>
    public IDictionary<object, object?> Properties { get; }

    /// <summary>
    /// Gets the <see cref="SelectorModel"/> instances.
    /// </summary>
    public IList<SelectorModel> Selectors { get; }

    /// <summary>
    /// Gets a collection of route values that must be present in the <see cref="RouteData.Values"/>
    /// for the corresponding page to be selected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value of <see cref="ViewEnginePath"/> is considered an implicit route value corresponding
    /// to the key <c>page</c>.
    /// </para>
    /// <para>
    /// The value of <see cref="AreaName"/> is considered an implicit route value corresponding
    /// to the key <c>area</c> when <see cref="AreaName"/> is not <c>null</c>.
    /// </para>
    /// <para>
    /// These entries will be implicitly added to <see cref="ActionDescriptor.RouteValues"/>
    /// when the action descriptor is created, but will not be visible in <see cref="RouteValues"/>.
    /// </para>
    /// </remarks>
    public IDictionary<string, string> RouteValues { get; }

    /// <summary>
    /// Gets or sets an <see cref="IOutboundParameterTransformer"/> that will be used to transform
    /// built-in route parameters such as <c>action</c>, <c>controller</c>, and <c>area</c> as well as
    /// additional parameters specified by <see cref="RouteValues"/> into static segments in the route template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This feature only applies when using endpoint routing.
    /// </para>
    /// </remarks>
    public IOutboundParameterTransformer? RouteParameterTransformer { get; set; }
}
