// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Binding;

internal class HttpContextFormDataProvider : FormDataProvider
{
    private string? _name;
    private ReadOnlyDictionary<string, string?>? _entries;

    public override string? Name => _name;

    public override IReadOnlyDictionary<string, string?> Entries => _entries ?? ReadOnlyDictionary<string, string?>.Empty;

    internal void SetFormState(IFormCollection form, string handler)
    {
        _name = handler;
        _entries = form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]).AsReadOnly();
    }
}
