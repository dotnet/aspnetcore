// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ResponseCompressionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseResponseCompression(this Microsoft.AspNetCore.Builder.IApplicationBuilder builder) { throw null; }
    }
    public static partial class ResponseCompressionServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCompression(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCompression(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.ResponseCompression.ResponseCompressionOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.ResponseCompression
{
    public partial class BrotliCompressionProvider : Microsoft.AspNetCore.ResponseCompression.ICompressionProvider
    {
        public BrotliCompressionProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions> options) { }
        public string EncodingName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool SupportsFlush { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.IO.Stream CreateStream(System.IO.Stream outputStream) { throw null; }
    }
    public partial class BrotliCompressionProviderOptions : Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>
    {
        public BrotliCompressionProviderOptions() { }
        public System.IO.Compression.CompressionLevel Level { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>.Value { get { throw null; } }
    }
    public partial class CompressionProviderCollection : System.Collections.ObjectModel.Collection<Microsoft.AspNetCore.ResponseCompression.ICompressionProvider>
    {
        public CompressionProviderCollection() { }
        public void Add(System.Type providerType) { }
        public void Add<TCompressionProvider>() where TCompressionProvider : Microsoft.AspNetCore.ResponseCompression.ICompressionProvider { }
    }
    public partial class GzipCompressionProvider : Microsoft.AspNetCore.ResponseCompression.ICompressionProvider
    {
        public GzipCompressionProvider(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions> options) { }
        public string EncodingName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool SupportsFlush { get { throw null; } }
        public System.IO.Stream CreateStream(System.IO.Stream outputStream) { throw null; }
    }
    public partial class GzipCompressionProviderOptions : Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>
    {
        public GzipCompressionProviderOptions() { }
        public System.IO.Compression.CompressionLevel Level { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>.Value { get { throw null; } }
    }
    public partial interface ICompressionProvider
    {
        string EncodingName { get; }
        bool SupportsFlush { get; }
        System.IO.Stream CreateStream(System.IO.Stream outputStream);
    }
    public partial interface IResponseCompressionProvider
    {
        bool CheckRequestAcceptsCompression(Microsoft.AspNetCore.Http.HttpContext context);
        Microsoft.AspNetCore.ResponseCompression.ICompressionProvider GetCompressionProvider(Microsoft.AspNetCore.Http.HttpContext context);
        bool ShouldCompressResponse(Microsoft.AspNetCore.Http.HttpContext context);
    }
    public partial class ResponseCompressionDefaults
    {
        public static readonly System.Collections.Generic.IEnumerable<string> MimeTypes;
        public ResponseCompressionDefaults() { }
    }
    public partial class ResponseCompressionMiddleware
    {
        public ResponseCompressionMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.ResponseCompression.IResponseCompressionProvider provider) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class ResponseCompressionOptions
    {
        public ResponseCompressionOptions() { }
        public bool EnableForHttps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IEnumerable<string> ExcludedMimeTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IEnumerable<string> MimeTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.ResponseCompression.CompressionProviderCollection Providers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ResponseCompressionProvider : Microsoft.AspNetCore.ResponseCompression.IResponseCompressionProvider
    {
        public ResponseCompressionProvider(System.IServiceProvider services, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.ResponseCompressionOptions> options) { }
        public bool CheckRequestAcceptsCompression(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public virtual Microsoft.AspNetCore.ResponseCompression.ICompressionProvider GetCompressionProvider(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
        public virtual bool ShouldCompressResponse(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
