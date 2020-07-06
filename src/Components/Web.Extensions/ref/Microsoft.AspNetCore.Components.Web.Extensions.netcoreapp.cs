// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public abstract partial class ProtectedBrowserStorage
    {
        protected ProtectedBrowserStorage(string storeName, Microsoft.JSInterop.IJSRuntime jsRuntime, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider) { }
        public System.Threading.Tasks.ValueTask DeleteAsync(string key) { throw null; }
        public System.Threading.Tasks.ValueTask<T> GetValueOrDefaultAsync<T>(string key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<T> GetValueOrDefaultAsync<T>(string purpose, string key) { throw null; }
        public System.Threading.Tasks.ValueTask SetAsync(string key, object value) { throw null; }
        public System.Threading.Tasks.ValueTask SetAsync(string purpose, string key, object value) { throw null; }
        public System.Threading.Tasks.ValueTask<(bool success, T result)> TryGetAsync<T>(string key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<(bool success, T result)> TryGetAsync<T>(string purpose, string key) { throw null; }
    }
    public partial class ProtectedLocalStorage : Microsoft.AspNetCore.Components.Web.Extensions.ProtectedBrowserStorage
    {
        public ProtectedLocalStorage(Microsoft.JSInterop.IJSRuntime jsRuntime, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider) : base (default(string), default(Microsoft.JSInterop.IJSRuntime), default(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider)) { }
    }
    public partial class ProtectedSessionStorage : Microsoft.AspNetCore.Components.Web.Extensions.ProtectedBrowserStorage
    {
        public ProtectedSessionStorage(Microsoft.JSInterop.IJSRuntime jsRuntime, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider) : base (default(string), default(Microsoft.JSInterop.IJSRuntime), default(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider)) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ProtectedBrowserStorageServiceCollectionExtensions
    {
        public static void AddProtectedBrowserStorage(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
}
