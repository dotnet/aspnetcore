// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace BasicWebSite;

public class StartupRequestLimitSize
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc()
            .AddNewtonsoftJson();
        services.ConfigureBaseWebSiteAuthPolicies();
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

            return next(httpContext);
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
        });
    }

    private class RequestBodySizeCheckingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly IHttpMaxRequestBodySizeFeature _maxRequestBodySizeFeature;
        private long _totalRead;

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
                && _innerStream.CanSeek && _innerStream.Length > _maxRequestBodySizeFeature.MaxRequestBodySize)
            {
                throw new InvalidOperationException("Request content size is greater than the limit size");
            }

            var read = _innerStream.Read(buffer, offset, count);
            _totalRead += read;

            if (_maxRequestBodySizeFeature.MaxRequestBodySize != null
                && _totalRead > _maxRequestBodySizeFeature.MaxRequestBodySize)
            {
                throw new InvalidOperationException("Request content size is greater than the limit size");
            }
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_maxRequestBodySizeFeature.MaxRequestBodySize != null
                && _innerStream.CanSeek && _innerStream.Length > _maxRequestBodySizeFeature.MaxRequestBodySize)
            {
                throw new InvalidOperationException("Request content size is greater than the limit size");
            }

            var read = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            _totalRead += read;

            if (_maxRequestBodySizeFeature.MaxRequestBodySize != null
                && _totalRead > _maxRequestBodySizeFeature.MaxRequestBodySize)
            {
                throw new InvalidOperationException("Request content size is greater than the limit size");
            }
            return read;
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

