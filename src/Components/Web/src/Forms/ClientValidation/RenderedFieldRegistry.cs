// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Per-form registry of the inputs that rendered under an <see cref="EditContext"/> in static
/// SSR, mapping each <see cref="FieldIdentifier"/> to the HTML <c>name</c> it rendered with.
/// Stored in <see cref="EditContext.Properties"/> keyed by <c>typeof(RenderedFieldRegistry)</c>.
/// Written by <c>InputBase</c> as inputs initialize and read by <see cref="ClientValidationData"/>
/// when it emits the client-validation payload.
/// </summary>
internal sealed class RenderedFieldRegistry
{
    private readonly Dictionary<FieldIdentifier, string> _fields = [];

    /// <summary>Gets the registered fields keyed by identifier, valued by rendered HTML name.</summary>
    public IReadOnlyDictionary<FieldIdentifier, string> Fields => _fields;

    /// <summary>Records that an input rendered for the field, with the HTML name it posts under.</summary>
    public void Register(in FieldIdentifier fieldIdentifier, string renderedName)
        => _fields[fieldIdentifier] = renderedName;

    /// <summary>Gets the registry for the context, creating and storing one if absent.</summary>
    public static RenderedFieldRegistry GetOrCreate(EditContext editContext)
    {
        if (editContext.Properties.TryGetValue(typeof(RenderedFieldRegistry), out var existing))
        {
            return (RenderedFieldRegistry)existing;
        }

        var registry = new RenderedFieldRegistry();
        editContext.Properties[typeof(RenderedFieldRegistry)] = registry;
        return registry;
    }

    /// <summary>Gets the registry for the context, or <see langword="null"/> if none was created.</summary>
    public static RenderedFieldRegistry? Get(EditContext editContext)
        => editContext.Properties.TryGetValue(typeof(RenderedFieldRegistry), out var existing)
            ? (RenderedFieldRegistry)existing
            : null;
}
