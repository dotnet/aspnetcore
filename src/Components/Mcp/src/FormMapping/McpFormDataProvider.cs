// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Mcp.FormMapping;

internal sealed class McpFormDataProvider
{
    public string? HandlerName { get; private set; }

    public IReadOnlyDictionary<string, StringValues> Entries { get; private set; }
        = ReadOnlyDictionary<string, StringValues>.Empty;

    public IFormFileCollection Files { get; private set; } = new FormFileCollection();

    public void Initialize(
        string handlerName,
        IReadOnlyDictionary<string, StringValues> entries,
        IFormFileCollection files)
    {
        ArgumentException.ThrowIfNullOrEmpty(handlerName);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(files);

        if (HandlerName is not null)
        {
            throw new InvalidOperationException("Form data has already been initialized for this rendering operation.");
        }

        HandlerName = handlerName;
        Entries = entries;
        Files = files;
    }
}
