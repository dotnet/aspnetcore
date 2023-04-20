// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The binding context associated with a given model binding operation.
/// </summary>
public sealed class ModelBindingContext
{
    internal ModelBindingContext(string name, string bindingContextId)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(bindingContextId);
        // We are initializing the root context, that can be a "named" root context, or the default context.
        // A named root context only provides a name, and that acts as the BindingId
        // A "default" root context does not provide a name, and instead it provides an explicit Binding ID.
        // The explicit binding ID matches that of the default handler, which is the URL Path.
        if (string.IsNullOrEmpty(name) ^ string.IsNullOrEmpty(bindingContextId))
        {
            throw new InvalidOperationException("A root binding context needs to provide a name and explicit binding context id or none.");
        }

        Name = name;
        BindingContextId = bindingContextId ?? name;
    }

    /// <summary>
    /// The context name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The computed identifier used to determine what parts of the app can bind data.
    /// </summary>
    public string BindingContextId { get; }
}
