// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Extensions
{
    /// <summary>
    /// Provides mechanisms for storing and retrieving data in the browser storage.
    /// </summary>
    public abstract class ProtectedBrowserStorage
    {
        private const string JsFunctionsPrefix = "Blazor._internal.protectedBrowserStorage";

        private readonly string _storeName;
        private readonly IJSRuntime _jsRuntime;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ConcurrentDictionary<string, IDataProtector> _cachedDataProtectorsByPurpose
            = new ConcurrentDictionary<string, IDataProtector>();

        // Stylistically, it doesn't matter at all what options we choose, since the values
        // will be opaque after data protection. All that matters is that some fixed set of
        // options exists and remains constant forever. We should choose whatever options
        // maximize the ability to round-trip .NET objects reliably.
        private readonly static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

        /// <summary>
        /// Constructs an instance of <see cref="ProtectedBrowserStorage"/>.
        /// </summary>
        /// <param name="storeName">The name of the store in which the data should be stored.</param>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
        /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/>.</param>
        protected ProtectedBrowserStorage(string storeName, IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Browser))
            {
                throw new InvalidOperationException($"{GetType()} cannot be used when running in WebAssembly.");
            }

            if (string.IsNullOrEmpty(storeName))
            {
                throw new ArgumentException("The value cannot be null or empty", nameof(storeName));
            }

            _storeName = storeName;
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
        }

        /// <summary>
        /// <para>
        /// Asynchronously stores the specified data.
        /// </para>
        /// <para>
        /// Since no data protection purpose is specified with this overload, the purpose is derived from <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a fixed set known at compile-time.
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
        /// <param name="purpose">A string that defines a scope for the data protection. The protected data can only be unprotected by code that specifies the same purpose.</param>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
        /// <param name="value">A JSON-serializable value to be stored.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public ValueTask SetAsync(string purpose, string key, object value)
        {
            if (string.IsNullOrEmpty(purpose))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(purpose));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(key));
            }

            return SetProtectedJsonAsync(key, Protect(purpose, value));
        }

        /// <summary>
        /// <para>
        /// Asynchronously retrieves the specified data.
        /// </para>
        /// <para>
        /// The first value in the <see cref="ValueTask"/> result indicates whether the data was retrieved successfully.
        /// </para>
        /// <para>
        /// Since no data protection purpose is specified with this overload, the purpose is derived from <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a fixed set known at compile-time.
        /// </para>
        /// </summary>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public ValueTask<(bool success, T result)> TryGetAsync<T>(string key)
            => TryGetAsync<T>(CreatePurposeFromKey(key), key);

        /// <summary>
        /// <para>
        /// Asynchronously retrieves the specified data.
        /// </para>
        /// <para>
        /// The first value in the <see cref="ValueTask"/> result indicates whether the data was retrieved successfully.
        /// </para>
        /// </summary>
        /// <param name="purpose">A string that defines a scope for the data protection. The protected data can only be unprotected if the same purpose was previously specified when calling <see cref="SetAsync(string, string, object)"/>.</param>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public async ValueTask<(bool success, T result)> TryGetAsync<T>(string purpose, string key)
        {
            var protectedJson = await GetProtectedJsonAsync(key);

            return protectedJson == null ? (false, default!) : (true, Unprotect<T>(purpose, protectedJson));
        }

        /// <summary>
        /// <para>
        /// Asynchronously retrieves the specified data.
        /// </para>
        /// <para>
        /// If no slot with the given <paramref name="key"/> exists, the <see langword="default" /> value for <typeparamref name="T"/> is returned.
        /// </para>
        /// <para>
        /// Since no data protection purpose is specified with this overload, the purpose is derived from <paramref name="key"/> and the store name. This is a good default purpose to use if the keys come from a fixed set known at compile-time.
        /// </para>
        /// </summary>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public ValueTask<T> GetValueOrDefaultAsync<T>(string key)
            => GetValueOrDefaultAsync<T>(CreatePurposeFromKey(key), key);

        /// <summary>
        /// <para>
        /// Asynchronously retrieves the specified data.
        /// </para>
        /// <para>
        /// If no slot with the given <paramref name="key"/> exists, the <see langword="default" /> value for <typeparamref name="T"/> is returned.
        /// </para>
        /// </summary>
        /// <param name="purpose">A string that defines a scope for the data protection. The protected data can only be unprotected if the same purpose was previously specified when calling <see cref="SetAsync(string, string, object)"/>.</param>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot to use.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public async ValueTask<T> GetValueOrDefaultAsync<T>(string purpose, string key)
        {
            var protectedJson = await GetProtectedJsonAsync(key);

            return protectedJson == null ? default! : Unprotect<T>(purpose, protectedJson);
        }

        /// <summary>
        /// Asynchronously deletes any data stored for the specified key.
        /// </summary>
        /// <param name="key">A <see cref="string"/> value specifying the name of the storage slot whose value should be deleted.</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public ValueTask DeleteAsync(string key)
            => _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.delete", _storeName, key);

        private string Protect(string purpose, object value)
        {
            var json = JsonSerializer.Serialize(value, options: SerializerOptions);
            var protector = GetOrCreateCachedProtector(purpose);

            return protector.Protect(json);
        }

        private T Unprotect<T>(string purpose, string protectedJson)
        {
            var protector = GetOrCreateCachedProtector(purpose);
            var json = protector.Unprotect(protectedJson);

            return JsonSerializer.Deserialize<T>(json, options: SerializerOptions)!;
        }

        private ValueTask SetProtectedJsonAsync(string key, string protectedJson)
           => _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.set", _storeName, key, protectedJson);

        private ValueTask<string> GetProtectedJsonAsync(string key)
            => _jsRuntime.InvokeAsync<string>($"{JsFunctionsPrefix}.get", _storeName, key);

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
}
