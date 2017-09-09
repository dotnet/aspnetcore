// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupRequestLimitSize
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.Use((httpContext, next) =>
            {
                var testHttpMaxRequestBodySizeFeature = new TestHttpMaxRequestBodySizeFeature();
                httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(
                    testHttpMaxRequestBodySizeFeature);

                httpContext.Request.Body = new RequestBodySizeCheckingStream(
                    httpContext.Request.Body,
                    testHttpMaxRequestBodySizeFeature);

                return next();
            });

            app.UseMvcWithDefaultRoute();
        }

        private class RequestBodySizeCheckingStream : Stream
        {
            private readonly Stream _innerStream;
            private readonly IHttpMaxRequestBodySizeFeature _maxRequestBodySizeFeature;

            public RequestBodySizeCheckingStream(
                Stream innerStream,
                IHttpMaxRequestBodySizeFeature maxRequestBodySizeFeature)
            {
                _innerStream = innerStream;
                _maxRequestBodySizeFeature = maxRequestBodySizeFeature;
            }
            public override bool CanRead => _innerStream.CanRead;

            public override bool CanSeek => _innerStream.CanSeek;

            public override bool CanWrite => _innerStream.CanWrite;

            public override long Length => _innerStream.Length;

            public override long Position
            {
                get { return _innerStream.Position; }
                set { _innerStream.Position = value; }
            }

            public override void Flush()
            {
                _innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_maxRequestBodySizeFeature.MaxRequestBodySize != null
                    && _innerStream.Length > _maxRequestBodySizeFeature.MaxRequestBodySize)
                {
                    throw new InvalidOperationException("Request content size is greater than the limit size");
                }

                return _innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _innerStream.Write(buffer, offset, count);
            }
        }

        private class TestHttpMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
        {
            public bool IsReadOnly => false;
            public long? MaxRequestBodySize { get; set; }
        }
    }
}

