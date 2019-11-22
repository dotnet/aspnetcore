// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ConcurrencyLimiterExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseConcurrencyLimiter(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    public partial class ConcurrencyLimiterMiddleware
    {
        public ConcurrencyLimiterMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.ConcurrencyLimiter.IQueuePolicy queue, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ConcurrencyLimiter.ConcurrencyLimiterOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class ConcurrencyLimiterOptions
    {
        public ConcurrencyLimiterOptions() { }
        public Microsoft.AspNetCore.Http.RequestDelegate OnRejected { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial interface IQueuePolicy
    {
        void OnExit();
        System.Threading.Tasks.ValueTask<bool> TryEnterAsync();
    }
    public partial class QueuePolicyOptions
    {
        public QueuePolicyOptions() { }
        public int MaxConcurrentRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RequestQueueLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class QueuePolicyServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddQueuePolicy(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.ConcurrencyLimiter.QueuePolicyOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddStackPolicy(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.ConcurrencyLimiter.QueuePolicyOptions> configure) { throw null; }
    }
}
