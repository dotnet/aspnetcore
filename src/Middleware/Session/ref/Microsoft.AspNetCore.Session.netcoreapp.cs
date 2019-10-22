// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class SessionMiddlewareExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseSession(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseSession(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.SessionOptions options) { throw null; }
    }
    public partial class SessionOptions
    {
        public SessionOptions() { }
        public Microsoft.AspNetCore.Http.CookieBuilder Cookie { get { throw null; } set { } }
        public System.TimeSpan IdleTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan IOTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.Session
{
    public partial class DistributedSession : Microsoft.AspNetCore.Http.ISession
    {
        public DistributedSession(Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, string sessionKey, System.TimeSpan idleTimeout, System.TimeSpan ioTimeout, System.Func<bool> tryEstablishSession, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, bool isNewSessionKey) { }
        public string Id { get { throw null; } }
        public bool IsAvailable { get { throw null; } }
        public System.Collections.Generic.IEnumerable<string> Keys { get { throw null; } }
        public void Clear() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Remove(string key) { }
        public void Set(string key, byte[] value) { }
        public bool TryGetValue(string key, out byte[] value) { throw null; }
    }
    public partial class DistributedSessionStore : Microsoft.AspNetCore.Session.ISessionStore
    {
        public DistributedSessionStore(Microsoft.Extensions.Caching.Distributed.IDistributedCache cache, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.AspNetCore.Http.ISession Create(string sessionKey, System.TimeSpan idleTimeout, System.TimeSpan ioTimeout, System.Func<bool> tryEstablishSession, bool isNewSessionKey) { throw null; }
    }
    public partial interface ISessionStore
    {
        Microsoft.AspNetCore.Http.ISession Create(string sessionKey, System.TimeSpan idleTimeout, System.TimeSpan ioTimeout, System.Func<bool> tryEstablishSession, bool isNewSessionKey);
    }
    public static partial class SessionDefaults
    {
        public static readonly string CookieName;
        public static readonly string CookiePath;
    }
    public partial class SessionFeature : Microsoft.AspNetCore.Http.Features.ISessionFeature
    {
        public SessionFeature() { }
        public Microsoft.AspNetCore.Http.ISession Session { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class SessionMiddleware
    {
        public SessionMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider, Microsoft.AspNetCore.Session.ISessionStore sessionStore, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.SessionOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SessionServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSession(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSession(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Builder.SessionOptions> configure) { throw null; }
    }
}
