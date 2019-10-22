// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public partial class IISServerOptions
    {
        public IISServerOptions() { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string AuthenticationDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool AutomaticAuthentication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaxRequestBodySize { get { throw null; } set { } }
    }
}
namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderIISExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseIIS(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.IIS
{
    public sealed partial class BadHttpRequestException : System.IO.IOException
    {
        internal BadHttpRequestException() { }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class HttpContextExtensions
    {
        [System.ObsoleteAttribute("This is obsolete and will be removed in a future version. Use GetServerVariable instead.")]
        public static string GetIISServerVariable(this Microsoft.AspNetCore.Http.HttpContext context, string variableName) { throw null; }
    }
    public partial class IISServerDefaults
    {
        public const string AuthenticationScheme = "Windows";
        public IISServerDefaults() { }
    }
}
namespace Microsoft.AspNetCore.Server.IIS.Core
{
    public partial class IISServerAuthenticationHandler : Microsoft.AspNetCore.Authentication.IAuthenticationHandler
    {
        public IISServerAuthenticationHandler() { }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> AuthenticateAsync() { throw null; }
        public System.Threading.Tasks.Task ChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public System.Threading.Tasks.Task ForbidAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties) { throw null; }
        public System.Threading.Tasks.Task InitializeAsync(Microsoft.AspNetCore.Authentication.AuthenticationScheme scheme, Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class ThrowingWasUpgradedWriteOnlyStream : Microsoft.AspNetCore.Server.IIS.Core.WriteOnlyStream
    {
        public ThrowingWasUpgradedWriteOnlyStream() { }
        public override void Flush() { }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public abstract partial class WriteOnlyStream : System.IO.Stream
    {
        protected WriteOnlyStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
    }
}
