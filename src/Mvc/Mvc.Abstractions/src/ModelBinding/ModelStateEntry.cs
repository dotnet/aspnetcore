// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// An entry in a <see cref="ModelStateDictionary"/>.
/// </summary>
public abstract class ModelStateEntry
{
    private ModelErrorCollection? _errors;

    /// <summary>
    /// Gets the raw value from the request associated with this entry.
    /// </summary>
    public object? RawValue { get; set; }

    /// <summary>
    /// Gets the set of values contained in <see cref="RawValue"/>, joined into a comma-separated string.
    /// </summary>
    public string? AttemptedValue { get; set; }

    /// <summary>
    /// Gets the <see cref="ModelErrorCollection"/> for this entry.
    /// </summary>
    public ModelErrorCollection Errors
    {
        get
        {
            if (_errors == null)
            {
                _errors = new ModelErrorCollection();
            }
            return _errors;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ModelValidationState"/> for this entry.
    /// </summary>
    public ModelValidationState ValidationState { get; set; }

    /// <summary>
    /// Gets a value that determines if the current instance of <see cref="ModelStateEntry"/> is a container node.
    /// Container nodes represent prefix nodes that aren't explicitly added to the
    /// <see cref="ModelStateDictionary"/>.
    /// </summary>
    public abstract bool IsContainerNode { get; }

    /// <summary>
    /// Gets the <see cref="ModelStateEntry"/> for a sub-property with the specified
    /// <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="propertyName">The property name to lookup.</param>
    /// <returns>
    /// The <see cref="ModelStateEntry"/> if a sub-property was found; otherwise <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method returns any existing entry, even those with <see cref="IsContainerNode"/> with value
    /// <see langword="true"/>.
    /// </remarks>
    public abstract ModelStateEntry? GetModelStateForProperty(string propertyName);

    /// <summary>
    /// Gets the <see cref="ModelStateEntry"/> values for sub-properties.
    /// </summary>
    /// <remarks>
    /// This property returns all existing entries, even those with <see cref="IsContainerNode"/> with value
    /// <see langword="true"/>.
    /// </remarks>
    public abstract IReadOnlyList<ModelStateEntry>? Children { get; }
}
