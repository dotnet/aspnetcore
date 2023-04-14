// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// The binding context associated with a given model binding operation.
/// </summary>
public class ModelBindingContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="ModelBindingContext"/>.
    /// </summary>
    /// <param name="name">The context name.</param>
    public ModelBindingContext(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// The context name.
    /// </summary>
    public string Name { get; }
}
