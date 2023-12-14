// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A type that represents a selector.
/// </summary>
public class SelectorModel
{
    /// <summary>
    /// Intializes a new <see cref="SelectorModel"/>.
    /// </summary>
    public SelectorModel()
    {
        ActionConstraints = new List<IActionConstraintMetadata>();
        EndpointMetadata = new List<object>();
    }

    /// <summary>
    /// Intializes a new <see cref="SelectorModel"/>.
    /// </summary>
    /// <param name="other">The <see cref="SelectorModel"/> to copy from.</param>
    public SelectorModel(SelectorModel other)
    {
        ArgumentNullException.ThrowIfNull(other);

        ActionConstraints = new List<IActionConstraintMetadata>(other.ActionConstraints);
        EndpointMetadata = new List<object>(other.EndpointMetadata);

        if (other.AttributeRouteModel != null)
        {
            AttributeRouteModel = new AttributeRouteModel(other.AttributeRouteModel);
        }
    }

    /// <summary>
    /// The <see cref="AttributeRouteModel"/>.
    /// </summary>
    public AttributeRouteModel? AttributeRouteModel { get; set; }

    /// <summary>
    /// The list of <see cref="IActionConstraintMetadata"/>.
    /// </summary>
    public IList<IActionConstraintMetadata> ActionConstraints { get; }

    /// <summary>
    /// Gets the <see cref="EndpointMetadata"/> associated with the <see cref="SelectorModel"/>.
    /// </summary>
    public IList<object> EndpointMetadata { get; }
}
