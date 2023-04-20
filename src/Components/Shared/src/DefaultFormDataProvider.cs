// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Binding;

internal class DefaultFormDataProvider : FormDataProvider
{
    private static readonly IReadOnlyDictionary<string, string?> Empty =
        new Dictionary<string, string?>().AsReadOnly();

    public override string? Name => null;

    public bool IsAvailable => Name != null;

    public override IReadOnlyDictionary<string, string?> Entries => Empty;
}
