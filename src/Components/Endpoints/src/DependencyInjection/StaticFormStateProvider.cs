// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection;

internal class StaticFormStateProvider : IFormStateProvider
{
    private static readonly IReadOnlyDictionary<string, string?> Empty = new Dictionary<string, string?>()
        .AsReadOnly();

    public string? Handler { get; internal set; }

    public bool IsAvailable { get; internal set; }

    public IReadOnlyDictionary<string, string?> Fields { get; private set; } = Empty;

    internal void SetFormState(HttpRequest request, string? handler)
    {
        Handler = handler;
        IsAvailable = true;
        Fields = request.Form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]).AsReadOnly();
    }
}
