// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

/// <summary>
/// Provides mechanisms for storing and retrieving data in the browser's
/// 'sessionStorage' collection.
///
/// This data will be scoped to the current browser tab. The data will be
/// discarded if the user closes the browser tab or closes the browser itself.
///
/// See: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage"/>.
/// </summary>
public sealed class ProtectedSessionStorage : ProtectedBrowserStorage
{
    /// <summary>
    /// Constructs an instance of <see cref="ProtectedSessionStorage"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    public ProtectedSessionStorage(IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
        : this(jsRuntime, dataProtectionProvider, jsonSerializerOptions: null)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="ProtectedSessionStorage"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    /// <param name="jsonOptions">The <see cref="JsonOptions"/>.</param>
    public ProtectedSessionStorage(
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<JsonOptions> jsonOptions)
        : this(jsRuntime, dataProtectionProvider, jsonOptions.Value.SerializerOptions)
    {
    }

    private ProtectedSessionStorage(
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions? jsonSerializerOptions)
        : base("sessionStorage", jsRuntime, dataProtectionProvider, jsonSerializerOptions)
    {
    }
}
