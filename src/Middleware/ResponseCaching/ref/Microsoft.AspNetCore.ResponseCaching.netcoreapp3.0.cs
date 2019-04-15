// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ResponseCachingExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseResponseCaching(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.AspNetCore.ResponseCaching
{
    public partial class ResponseCachingFeature : Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature
    {
        public ResponseCachingFeature() { }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
    }
    public partial class ResponseCachingMiddleware
    {
        public ResponseCachingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.ObjectPool.ObjectPoolProvider poolProvider) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class ResponseCachingOptions
    {
        public ResponseCachingOptions() { }
        public long MaximumBodySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long SizeLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool UseCaseSensitivePaths { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public partial class CachedResponse
    {
        public CachedResponse() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.DateTimeOffset Created { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CachedVaryByRules
    {
        public CachedVaryByRules() { }
        public Microsoft.Extensions.Primitives.StringValues Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Primitives.StringValues QueryKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string VaryByKeyPrefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ResponseCachingServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCaching(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCaching(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> configureOptions) { throw null; }
    }
}
