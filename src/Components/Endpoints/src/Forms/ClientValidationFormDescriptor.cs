// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal sealed class ClientValidationFormDescriptor
{
    public ClientValidationFormDescriptor(IReadOnlyList<ClientValidationFieldDescriptor> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    public IReadOnlyList<ClientValidationFieldDescriptor> Fields { get; }
}
