// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

// Describes a single client-side validation rule including the resolved error message.
internal sealed class ClientValidationRuleDescriptor
{
    public ClientValidationRuleDescriptor(
        string name,
        string errorMessage,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(errorMessage);

        Name = name;
        ErrorMessage = errorMessage;
        Parameters = parameters;
    }

    public string Name { get; }

    public string ErrorMessage { get; }

    public IReadOnlyDictionary<string, string>? Parameters { get; }
}
