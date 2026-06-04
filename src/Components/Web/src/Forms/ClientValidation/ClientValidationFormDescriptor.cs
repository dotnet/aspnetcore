// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Describes the client-side validation data for one form: the set of validated fields
/// and their rules.
/// </summary>
public sealed class ClientValidationFormDescriptor
{
    /// <summary>
    /// Creates a descriptor with the given field descriptors.
    /// </summary>
    public ClientValidationFormDescriptor(IReadOnlyList<ClientValidationFieldDescriptor> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    /// <summary>
    /// Per-field client-side validation data.
    /// </summary>
    public IReadOnlyList<ClientValidationFieldDescriptor> Fields { get; }
}
