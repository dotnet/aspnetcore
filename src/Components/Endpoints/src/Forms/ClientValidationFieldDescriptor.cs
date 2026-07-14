// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal sealed class ClientValidationFieldDescriptor
{
    public ClientValidationFieldDescriptor(
        string name,
        IReadOnlyList<ClientValidationRuleDescriptor> rules)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(rules);
        Name = name;
        Rules = rules;
    }

    /// <summary>
    /// Field name as it appears in form posts.
    /// Dotted path for nested fields (e.g. <c>Address.Street</c>).
    /// </summary>
    public string Name { get; }

    public IReadOnlyList<ClientValidationRuleDescriptor> Rules { get; }
}
