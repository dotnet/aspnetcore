// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

/// <summary>
/// Provides mechanisms for storing and retrieving data in the browser's
/// 'localStorage' collection.
///
/// This data will be scoped to the current user's browser, shared across
/// all tabs. The data will persist across browser restarts.
///
/// See: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/localStorage"/>.
/// </summary>
public sealed class ProtectedLocalStorage : ProtectedBrowserStorage
{
    /// <summary>
    /// Constructs an instance of <see cref="ProtectedLocalStorage"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    public ProtectedLocalStorage(IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
        : this(jsRuntime, dataProtectionProvider, jsonSerializerOptions: null)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="ProtectedLocalStorage"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    /// <param name="jsonOptions">The <see cref="JsonOptions"/>.</param>
    public ProtectedLocalStorage(
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<JsonOptions> jsonOptions)
        : this(jsRuntime, dataProtectionProvider, jsonOptions.Value.SerializerOptions)
    {
    }

    private ProtectedLocalStorage(
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions? jsonSerializerOptions)
        : base("localStorage", jsRuntime, dataProtectionProvider, jsonSerializerOptions)
    {
    }
}
