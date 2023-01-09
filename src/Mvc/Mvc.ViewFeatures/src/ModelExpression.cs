// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Describes an <see cref="System.Linq.Expressions.Expression"/> passed to a tag helper.
/// </summary>
public sealed class ModelExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelExpression"/> class.
    /// </summary>
    /// <param name="name">
    /// String representation of the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </param>
    /// <param name="modelExplorer">
    /// Includes the model and metadata about the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </param>
    public ModelExpression(string name, ModelExplorer modelExplorer)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(modelExplorer);

        Name = name;
        ModelExplorer = modelExplorer;
    }

    /// <summary>
    /// String representation of the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Metadata about the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </summary>
    public ModelMetadata Metadata => ModelExplorer.Metadata;

    /// <summary>
    /// Gets the model object for the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </summary>
    /// <remarks>
    /// Getting <see cref="Model"/> will evaluate a compiled version of the original
    /// <see cref="System.Linq.Expressions.Expression"/>.
    /// </remarks>
    public object Model => ModelExplorer.Model;

    /// <summary>
    /// Gets the model explorer for the <see cref="System.Linq.Expressions.Expression"/> of interest.
    /// </summary>
    /// <remarks>
    /// Getting <see cref="ModelExplorer.Model"/> will evaluate a compiled version of the original
    /// <see cref="System.Linq.Expressions.Expression"/>.
    /// </remarks>
    public ModelExplorer ModelExplorer { get; }
}
