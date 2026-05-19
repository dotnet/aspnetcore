// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A read-only collection of <see cref="ModelMetadata"/> objects which represent model properties.
/// </summary>
public class ModelPropertyCollection : ReadOnlyCollection<ModelMetadata>
{
    /// <summary>
    /// Creates a new <see cref="ModelPropertyCollection"/>.
    /// </summary>
    /// <param name="properties">The properties.</param>
    public ModelPropertyCollection(IEnumerable<ModelMetadata> properties)
        : base(properties.ToList())
    {
    }

    /// <summary>
    /// Gets a <see cref="ModelMetadata"/> instance for the property corresponding to <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="propertyName">
    /// The property name. Property names are compared using <see cref="StringComparison.Ordinal"/>.
    /// </param>
    /// <returns>
    /// The <see cref="ModelMetadata"/> instance for the property specified by <paramref name="propertyName"/>, or
    /// <c>null</c> if no match can be found.
    /// </returns>
    public ModelMetadata? this[string propertyName]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(propertyName);

            for (var i = 0; i < Items.Count; i++)
            {
                var property = Items[i];
                if (string.Equals(property.PropertyName, propertyName, StringComparison.Ordinal))
                {
                    return property;
                }
            }

            return null;
        }
    }
}
