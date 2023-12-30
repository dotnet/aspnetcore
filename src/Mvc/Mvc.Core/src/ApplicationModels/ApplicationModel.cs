// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A model for configuring controllers in an MVC application.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name}, Controllers = {Controllers.Count}, Filters = {Filters.Count}")]
public class ApplicationModel : IPropertyModel, IFilterModel, IApiExplorerModel
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationModel"/>.
    /// </summary>
    public ApplicationModel()
    {
        ApiExplorer = new ApiExplorerModel();
        Controllers = new List<ControllerModel>();
        Filters = new List<IFilterMetadata>();
        Properties = new Dictionary<object, object?>();
    }

    /// <summary>
    /// Gets or sets the <see cref="ApiExplorerModel"/> for the application.
    /// </summary>
    /// <remarks>
    /// <see cref="ApplicationModel.ApiExplorer"/> allows configuration of default settings
    /// for ApiExplorer that apply to all actions unless overridden by
    /// <see cref="ControllerModel.ApiExplorer"/> or <see cref="ActionModel.ApiExplorer"/>.
    ///
    /// If using <see cref="ApplicationModel.ApiExplorer"/> to set <see cref="ApiExplorerModel.IsVisible"/> to
    /// <c>true</c>, this setting will only be honored for actions which use attribute routing.
    /// </remarks>
    public ApiExplorerModel ApiExplorer { get; set; }

    /// <summary>
    /// Gets the <see cref="ControllerModel"/> instances.
    /// </summary>
    public IList<ControllerModel> Controllers { get; }

    /// <summary>
    /// Gets the global <see cref="IFilterMetadata"/> instances.
    /// </summary>
    public IList<IFilterMetadata> Filters { get; }

    /// <summary>
    /// Gets a set of properties associated with all actions.
    /// These properties will be copied to <see cref="Abstractions.ActionDescriptor.Properties"/>.
    /// </summary>
    public IDictionary<object, object?> Properties { get; }
}
