// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

/// <summary>
/// Provides mechanisms for storing and retrieving data in the browser storage.
/// </summary>
public abstract class ProtectedBrowserStorage
{
    private readonly string _storeName;
    private readonly IJSRuntime _jsRuntime;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IServiceProvider? _services;
    private readonly ConcurrentDictionary<string, IDataProtector> _cachedDataProtectorsByPurpose
        = new ConcurrentDictionary<string, IDataProtector>(StringComparer.Ordinal);

    /// <summary>
    /// Constructs an instance of <see cref="ProtectedBrowserStorage"/>.
    /// </summary>
    /// <param name="storeName">The name of the store in which the data should be stored.</param>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
    private protected ProtectedBrowserStorage(string storeName, IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
        : this(storeName, jsRuntime, dataProtectionProvider, JsonSerializerOptionsProvider.Options, services: null)
    {
    }

    // Stored values are application types, so an application whose only serializer is generated has
    // to be able to supply their contracts. The options are taken as a dependency rather than read
    // from a static so that this stays ordinary DI and each container keeps its own. The service
    // provider is what the generic overloads resolve ProtectedBrowserStorageSerializer<T> from; it is
    // absent when an instance is constructed directly rather than resolved, in which case values are
    // always serialized as JSON.
    private protected ProtectedBrowserStorage(
        string storeName,
        IJSRuntime jsRuntime,
        IDataProtectionProvider dataProtectionProvider,
        JsonSerializerOptions serializerOptions,
        IServiceProvider? services)
    {
        // Performing data protection on the client would give users a false sense of security, so we'll prevent this.
        if (OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException($"{GetType()} cannot be used when running in a browser.");
        }

        ArgumentException.ThrowIfNullOrEmpty(storeName);

        _storeName = storeName;
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _services = services;
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
    /// Asynchronously stores the supplied data.
    /// </para>
    /// <para>
    /// Since no data protection purpose is specified with this overload, the purpose is derived from
    /// <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a
    /// fixed set known at compile-time.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The type of the value to store.</typeparam>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <param name="value">The value to be stored.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <remarks>
    /// The value is serialized using the registered <see cref="ProtectedBrowserStorageSerializer{T}"/>
    /// for <typeparamref name="TValue"/> if there is one, and as JSON otherwise.
    /// </remarks>
    public ValueTask SetAsync<TValue>(string key, TValue value)
        => SetAsync(CreatePurposeFromKey(key), key, value);

    /// <summary>
    /// Asynchronously stores the supplied data.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to store.</typeparam>
    /// <param name="purpose">
    /// A string that defines a scope for the data protection. The protected data can only
    /// be unprotected by code that specifies the same purpose.
    /// </param>
    /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
    /// <param name="value">The value to be stored.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <remarks>
    /// The value is serialized using the registered <see cref="ProtectedBrowserStorageSerializer{T}"/>
    /// for <typeparamref name="TValue"/> if there is one, and as JSON otherwise.
    /// </remarks>
    public ValueTask SetAsync<TValue>(string purpose, string key, TValue value)
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
        => _jsRuntime.InvokeVoidAsync($"{_storeName}.removeItem", key);

    private string Protect(string purpose, object value)
    {
        var json = JsonSerializer.Serialize(value, options: _serializerOptions);
        var protector = GetOrCreateCachedProtector(purpose);

        return protector.Protect(json);
    }

    // TValue is a static type argument at every call site, so the closed generic service lookup is
    // rooted by the caller. That is what keeps custom serializers usable in a trimmed or native build
    // without the framework reflecting over the value's runtime type.
    //
    // The serializer type is experimental, but the SetAsync/GetAsync overloads that reach these
    // helpers are not: with no serializer registered they behave exactly like the object-typed ones,
    // so binding to them must not raise a diagnostic for callers that never opt in.
#pragma warning disable ASPNETCORE9004
    private string Protect<TValue>(string purpose, TValue value)
    {
        var serializer = _services?.GetService<ProtectedBrowserStorageSerializer<TValue>>();
        var data = serializer is null
            ? JsonSerializer.Serialize(
                value,
                _serializerOptions.GetTypeInfo(value?.GetType() ?? typeof(TValue)))
            : serializer.Serialize(value);
        var protector = GetOrCreateCachedProtector(purpose);

        return protector.Protect(data);
    }

    private TValue Unprotect<TValue>(string purpose, string protectedJson)
    {
        var protector = GetOrCreateCachedProtector(purpose);
        var data = protector.Unprotect(protectedJson);
        var serializer = _services?.GetService<ProtectedBrowserStorageSerializer<TValue>>();

        return serializer is null
            ? (TValue)JsonSerializer.Deserialize(data, _serializerOptions.GetTypeInfo(typeof(TValue)))!
            : serializer.Deserialize(data);
    }
#pragma warning restore ASPNETCORE9004

    private ValueTask SetProtectedJsonAsync(string key, string protectedJson)
       => _jsRuntime.InvokeVoidAsync($"{_storeName}.setItem", key, protectedJson);

    private ValueTask<string?> GetProtectedJsonAsync(string key)
        => _jsRuntime.InvokeAsync<string?>($"{_storeName}.getItem", key);

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
