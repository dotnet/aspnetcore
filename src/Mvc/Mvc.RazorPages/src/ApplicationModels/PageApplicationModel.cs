// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Application model component for RazorPages.
/// </summary>
public class PageApplicationModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="PageApplicationModel"/>.
    /// </summary>
    public PageApplicationModel(
        PageActionDescriptor actionDescriptor,
        TypeInfo handlerType,
        IReadOnlyList<object> handlerAttributes)
        : this(actionDescriptor, handlerType, handlerType, handlerAttributes)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PageApplicationModel"/>.
    /// </summary>
    public PageApplicationModel(
        PageActionDescriptor actionDescriptor,
        TypeInfo declaredModelType,
        TypeInfo handlerType,
        IReadOnlyList<object> handlerAttributes)
    {
        ActionDescriptor = actionDescriptor ?? throw new ArgumentNullException(nameof(actionDescriptor));
        DeclaredModelType = declaredModelType;
        HandlerType = handlerType;

        Filters = new List<IFilterMetadata>();
        Properties = new CopyOnWriteDictionary<object, object?>(
            actionDescriptor.Properties,
            EqualityComparer<object>.Default);
        HandlerMethods = new List<PageHandlerModel>();
        HandlerProperties = new List<PagePropertyModel>();
        HandlerTypeAttributes = handlerAttributes;
        EndpointMetadata = new List<object>(ActionDescriptor.EndpointMetadata ?? Array.Empty<object>());
    }

    /// <summary>
    /// A copy constructor for <see cref="PageApplicationModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="PageApplicationModel"/> to copy from.</param>
    public PageApplicationModel(PageApplicationModel other)
    {
        ArgumentNullException.ThrowIfNull(other);

        ActionDescriptor = other.ActionDescriptor;
        HandlerType = other.HandlerType;
        PageType = other.PageType;
        ModelType = other.ModelType;

        Filters = new List<IFilterMetadata>(other.Filters);
        Properties = new Dictionary<object, object?>(other.Properties);

        HandlerMethods = new List<PageHandlerModel>(other.HandlerMethods.Select(m => new PageHandlerModel(m)));
        HandlerProperties = new List<PagePropertyModel>(other.HandlerProperties.Select(p => new PagePropertyModel(p)));
        HandlerTypeAttributes = other.HandlerTypeAttributes;
        EndpointMetadata = new List<object>(other.EndpointMetadata);
    }

    /// <summary>
    /// Gets the <see cref="PageActionDescriptor"/>.
    /// </summary>
    public PageActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// Gets the application root relative path for the page.
    /// </summary>
    public string RelativePath => ActionDescriptor.RelativePath;

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
    public string ViewEnginePath => ActionDescriptor.ViewEnginePath;

    /// <summary>
    /// Gets the area name.
    /// </summary>
    public string? AreaName => ActionDescriptor.AreaName;

    /// <summary>
    /// Gets the route template for the page.
    /// </summary>
    public string? RouteTemplate => ActionDescriptor.AttributeRouteInfo?.Template;

    /// <summary>
    /// Gets the applicable <see cref="IFilterMetadata"/> instances.
    /// </summary>
    public IList<IFilterMetadata> Filters { get; }

    /// <summary>
    /// Stores arbitrary metadata properties associated with the <see cref="PageApplicationModel"/>.
    /// </summary>
    public IDictionary<object, object?> Properties { get; }

    /// <summary>
    /// Gets or sets the <see cref="TypeInfo"/> of the Razor page.
    /// </summary>
    public TypeInfo PageType { get; set; } = default!;

    /// <summary>
    /// Gets the declared model <see cref="TypeInfo"/> of the model for the page.
    /// Typically this <see cref="TypeInfo"/> will be the type specified by the @model directive
    /// in the razor page.
    /// </summary>
    public TypeInfo? DeclaredModelType { get; }

    /// <summary>
    /// Gets or sets the runtime model <see cref="TypeInfo"/> of the model for the razor page.
    /// This is the <see cref="TypeInfo"/> that will be used at runtime to instantiate and populate
    /// the model property of the page.
    /// </summary>
    public TypeInfo? ModelType { get; set; }

    /// <summary>
    /// Gets the <see cref="TypeInfo"/> of the handler.
    /// </summary>
    public TypeInfo HandlerType { get; }

    /// <summary>
    /// Gets the sequence of attributes declared on <see cref="HandlerType"/>.
    /// </summary>
    public IReadOnlyList<object> HandlerTypeAttributes { get; }

    /// <summary>
    /// Gets the sequence of <see cref="PageHandlerModel"/> instances.
    /// </summary>
    public IList<PageHandlerModel> HandlerMethods { get; }

    /// <summary>
    /// Gets the sequence of <see cref="PagePropertyModel"/> instances on <see cref="PageHandlerModel"/>.
    /// </summary>
    public IList<PagePropertyModel> HandlerProperties { get; }

    /// <summary>
    /// Gets the endpoint metadata for this action.
    /// </summary>
    public IList<object> EndpointMetadata { get; }
}
