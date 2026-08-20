// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Platform;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

/// <summary>
/// Provides mechanisms for storing and retrieving data in the browser storage.
/// </summary>
public abstract class ProtectedBrowserStorage
{
    private readonly string _storeName;
    // The IJSRuntime path preserves the existing public constructors. Framework DI uses Storage.
    private readonly IJSRuntime? _jsRuntime;
    private readonly Storage? _storage;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ConcurrentDictionary<string, IDataProtector> _cachedDataProtectorsByPurpose
        = new ConcurrentDictionary<string, IDataProtector>(StringComparer.Ordinal);

    /// <summary>
    /// Constructs an instance of <see cref="ProtectedBrowserStorage"/>.
    /// </summary>
    /// <param name="storeName">The name of the store in which the data should be stored.</param>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    private protected ProtectedBrowserStorage(string storeName, IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
        : this(storeName, jsRuntime, dataProtectionProvider, JsonSerializerOptionsProvider.Options)
    {
    }

    internal ProtectedBrowserStorage(
        string storeName,
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions serializerOptions)
        : this(storeName, jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime)), storage: null, dataProtectionProvider, serializerOptions)
    {
    }

    internal ProtectedBrowserStorage(
        string storeName,
        Storage storage,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions serializerOptions)
        : this(storeName, jsRuntime: null, storage ?? throw new ArgumentNullException(nameof(storage)), dataProtectionProvider, serializerOptions)
    {
    }

    private ProtectedBrowserStorage(
        string storeName,
        IJSRuntime? jsRuntime,
        Storage? storage,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions serializerOptions)
    {
        // Performing data protection on the client would give users a false sense of security, so we'll prevent this.
        if (OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException($"{GetType()} cannot be used when running in a browser.");
        }

        ArgumentException.ThrowIfNullOrEmpty(storeName);

        _storeName = storeName;
        _jsRuntime = jsRuntime;
        _storage = storage;
        _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
    }

    /// <summary>
    /// <para>
    /// Asynchronously stores the specified data.
    /// </para>
    /// <para>
    /// Since no data protection purpose is specified with this overload, the purpose is derived from
    /// <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a
    /// fixed set known at compile-time.
    /// </para>
    /// </summary>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <param name="value">A JSON-serializable value to be stored.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask SetAsync(string key, object value)
        => SetAsync(CreatePurposeFromKey(key), key, value);

    /// <summary>
    /// Asynchronously stores the supplied data.
    /// </summary>
    /// <param name="purpose">
    /// A string that defines a scope for the data protection. The protected data can only
    /// be unprotected by code that specifies the same purpose.
    /// </param>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <param name="value">A JSON-serializable value to be stored.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask SetAsync(string purpose, string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return SetProtectedJsonAsync(key, Protect(purpose, value));
    }

    /// <summary>
    /// <para>
    /// Asynchronously retrieves the specified data.
    /// </para>
    /// <para>
    /// Since no data protection purpose is specified with this overload, the purpose is derived from
    /// <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a
    /// fixed set known at compile-time.
    /// </para>
    /// </summary>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask<ProtectedBrowserStorageResult<TValue>> GetAsync<TValue>(string key)
        => GetAsync<TValue>(CreatePurposeFromKey(key), key);

    /// <summary>
    /// <para>
    /// Asynchronously retrieves the specified data.
    /// </para>
    /// </summary>
    /// <param name="purpose">
    /// A string that defines a scope for the data protection. The protected data can only
    /// be unprotected if the same purpose was previously specified when calling
    /// <see cref="SetAsync(string, string, object)"/>.
    /// </param>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask<ProtectedBrowserStorageResult<TValue>> GetAsync<TValue>(string purpose, string key)
    {
        var protectedJson = await GetProtectedJsonAsync(key);

        return protectedJson == null ?
            new ProtectedBrowserStorageResult<TValue>(false, default) :
            new ProtectedBrowserStorageResult<TValue>(true, Unprotect<TValue>(purpose, protectedJson));
    }

    /// <summary>
    /// Asynchronously deletes any data stored for the specified key.
    /// </summary>
    /// <param name="key">
    /// A <see cref="string"/> value specifying the name of the storage slot whose value should be deleted.
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask DeleteAsync(string key)
        => _storage is null
            ? _jsRuntime!.InvokeVoidAsync($"{_storeName}.removeItem", key)
            : _storage.RemoveItemAsync(key);

    private string Protect(string purpose, object value)
    {
        var typeInfo = _serializerOptions.GetTypeInfo(value?.GetType() ?? typeof(object));
        var json = JsonSerializer.Serialize(value, typeInfo);
        var protector = GetOrCreateCachedProtector(purpose);

        return protector.Protect(json);
    }

    private TValue Unprotect<TValue>(string purpose, string protectedJson)
    {
        var protector = GetOrCreateCachedProtector(purpose);
        var json = protector.Unprotect(protectedJson);

        var typeInfo = _serializerOptions.GetTypeInfo(typeof(TValue));
        return (TValue)JsonSerializer.Deserialize(json, typeInfo)!;
    }

    private ValueTask SetProtectedJsonAsync(string key, string protectedJson)
        => _storage is null
            ? _jsRuntime!.InvokeVoidAsync($"{_storeName}.setItem", key, protectedJson)
            : _storage.SetItemAsync(key, protectedJson);

    private ValueTask<string?> GetProtectedJsonAsync(string key)
        => _storage is null
            ? _jsRuntime!.InvokeAsync<string?>($"{_storeName}.getItem", key)
            : _storage.GetItemAsync(key);

    // IDataProtect isn't disposable, so we're fine holding these indefinitely.
    // Only a bounded number of them will be created, as the 'key' values should
    // come from a bounded set known at compile-time. There's no use case for
    // letting runtime data determine the 'key' values.
    private IDataProtector GetOrCreateCachedProtector(string purpose)
        => _cachedDataProtectorsByPurpose.GetOrAdd(
            purpose,
            _dataProtectionProvider.CreateProtector);

    private string CreatePurposeFromKey(string key)
        => $"{GetType().FullName}:{_storeName}:{key}";
}
