// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class RequestThrottlingExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestThrottling(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.AspNetCore.RequestThrottling
{
    public partial interface IQueuePolicy
    {
        void OnExit();
        System.Threading.Tasks.Task<bool> TryEnterAsync();
    }
    public partial class QueuePolicyOptions
    {
        public QueuePolicyOptions() { }
        public int MaxConcurrentRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RequestQueueLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class RequestThrottlingMiddleware
    {
        public RequestThrottlingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.RequestThrottling.IQueuePolicy queue, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.RequestThrottling.RequestThrottlingOptions> options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class RequestThrottlingOptions
    {
        public RequestThrottlingOptions() { }
        public Microsoft.AspNetCore.Http.RequestDelegate OnRejected { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class QueuePolicyServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddStackQueue(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.RequestThrottling.QueuePolicyOptions> configure) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTailDropQueue(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.RequestThrottling.QueuePolicyOptions> configure) { throw null; }
    }
}
