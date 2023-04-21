// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.Binding;

internal class HttpContextFormDataProvider : FormDataProvider, IHostEnvironmentFormDataProvider
{
    private string? _name;
    private IReadOnlyDictionary<string, string?>? _entries;

    public override string? Name => _name;

    public override IReadOnlyDictionary<string, string?> Entries => _entries ?? ReadOnlyDictionary<string, string?>.Empty;

    void IHostEnvironmentFormDataProvider.SetFormData(string name, IReadOnlyDictionary<string, string?> form)
    {
        _name = name;
        _entries = form;
    }
}
