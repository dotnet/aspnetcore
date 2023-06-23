// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpContextFormDataProvider : FormDataProvider, IHostEnvironmentFormDataProvider
{
    private string? _name;
    private IReadOnlyDictionary<string, StringValues>? _entries;

    public override string? Name => _name;

    public override IReadOnlyDictionary<string, StringValues> Entries => _entries ?? ReadOnlyDictionary<string, StringValues>.Empty;

    public FormOptions FormOptions { get; private set; } = null!;

    internal void SetFormOptions(FormOptions formOptions)
    {
        FormOptions = formOptions;
    }

    void IHostEnvironmentFormDataProvider.SetFormData(string name, IReadOnlyDictionary<string, StringValues> form)
    {
        _name = name;
        _entries = form;
    }
}
