// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// The binding context associated with a given model binding operation.
/// </summary>
public class ModelBindingContext
{
    // Default binder
    // Name = URL.Path
    // FormAction = ""

    // Named from default binder
    // Name = <<handler>> (<<Path>> -> <<handler>>)
    // FormAction = ?handler=<<handler>> ("" -> ?handler=<<handler>>)

    // Named binder
    // Name = <<handler>>
    // FormAction = ?handler=<<handler>>

    // Nested named binder
    // Name = <<parent>>.<<handler>>
    // FormAction = ?handler=<<parent>>.<<handler>>
    public ModelBindingContext(string name, string? bindingId = null)
    {
        // We are initializing the root context, that can be a "named" root context, or the default context.
        // A named root context only provides a name, and that acts as the BindingId
        // A "default" root context does not provide a name, and instead it provides an explicit Binding ID.
        // The explicit binding ID matches that of the default handler, which is the URL Path.
        if (!(string.IsNullOrEmpty(name) ^ string.IsNullOrEmpty(bindingId)))
        {
            throw new InvalidOperationException("A root binding context needs to provide either a name or explicit binding ID.");
        }

        Name = name;
        BindingId = bindingId ?? name;
    }

    /// <summary>
    /// The context name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The computed identifier used to determine what parts of the app can bind data.
    /// </summary>
    public string BindingId { get; }
}
