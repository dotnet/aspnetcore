// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class RequestThrottlingExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestThrottling(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.Aspnetcore.RequestThrottling
{
    public partial class RequestThrottlingMiddleware
    {
        public RequestThrottlingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.RequestThrottling.RequestThrottlingOptions> options) { }
        public int ConcurrentRequests { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.RequestThrottling
{
    public partial class RequestThrottlingOptions
    {
        public RequestThrottlingOptions() { }
        public int MaxConcurrentRequests { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
