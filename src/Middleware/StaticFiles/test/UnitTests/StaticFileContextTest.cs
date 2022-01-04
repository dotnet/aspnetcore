// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticFiles;

public class StaticFileContextTest
{
    [Fact]
    public void LookupFileInfo_ReturnsFalse_IfFileDoesNotExist()
    {
        // Arrange
        var options = new StaticFileOptions();
        var httpContext = new DefaultHttpContext();
        var pathString = PathString.Empty;
        var validateResult = StaticFileMiddleware.ValidatePath(httpContext, pathString, out var subPath);
        var contentTypeResult = StaticFileMiddleware.LookupContentType(new FileExtensionContentTypeProvider(), options, subPath, out var contentType);
        var context = new StaticFileContext(httpContext, options, NullLogger.Instance, new TestFileProvider(), contentType, subPath);

        // Act
        var lookupResult = context.LookupFileInfo();

        // Assert
        Assert.True(validateResult);
        Assert.False(contentTypeResult);
        Assert.False(lookupResult);
    }

    [Fact]
    public void LookupFileInfo_ReturnsTrue_IfFileExists()
    {
        // Arrange
        var options = new StaticFileOptions();
        var fileProvider = new TestFileProvider();
        fileProvider.AddFile("/foo.txt", new TestFileInfo
        {
            LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
        });
        var pathString = new PathString("/test");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = new PathString("/test/foo.txt");
        var validateResult = StaticFileMiddleware.ValidatePath(httpContext, pathString, out var subPath);
        var contentTypeResult = StaticFileMiddleware.LookupContentType(new FileExtensionContentTypeProvider(), options, subPath, out var contentType);

        var context = new StaticFileContext(httpContext, options, NullLogger.Instance, fileProvider, contentType, subPath);

        // Act
        var result = context.LookupFileInfo();

        // Assert
        Assert.True(validateResult);
        Assert.True(contentTypeResult);
        Assert.True(result);
    }

    [Fact]
    public async Task EnablesHttpsCompression_IfMatched()
    {
        var options = new StaticFileOptions();
        var fileProvider = new TestFileProvider();
        fileProvider.AddFile("/foo.txt", new TestFileInfo
        {
            LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
        });
        var pathString = new PathString("/test");
        var httpContext = new DefaultHttpContext();
        var httpsCompressionFeature = new TestHttpsCompressionFeature();
        httpContext.Features.Set<IHttpsCompressionFeature>(httpsCompressionFeature);
        httpContext.Request.Path = new PathString("/test/foo.txt");
        var validateResult = StaticFileMiddleware.ValidatePath(httpContext, pathString, out var subPath);
        var contentTypeResult = StaticFileMiddleware.LookupContentType(new FileExtensionContentTypeProvider(), options, subPath, out var contentType);

        var context = new StaticFileContext(httpContext, options, NullLogger.Instance, fileProvider, contentType, subPath);

        var result = context.LookupFileInfo();
        Assert.True(validateResult);
        Assert.True(contentTypeResult);
        Assert.True(result);

        await context.SendAsync();

        Assert.Equal(HttpsCompressionMode.Compress, httpsCompressionFeature.Mode);
    }

    [Fact]
    public void SkipsHttpsCompression_IfNotMatched()
    {
        var options = new StaticFileOptions();
        var fileProvider = new TestFileProvider();
        fileProvider.AddFile("/foo.txt", new TestFileInfo
        {
            LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
        });
        var pathString = new PathString("/test");
        var httpContext = new DefaultHttpContext();
        var httpsCompressionFeature = new TestHttpsCompressionFeature();
        httpContext.Features.Set<IHttpsCompressionFeature>(httpsCompressionFeature);
        httpContext.Request.Path = new PathString("/test/bar.txt");
        var validateResult = StaticFileMiddleware.ValidatePath(httpContext, pathString, out var subPath);
        var contentTypeResult = StaticFileMiddleware.LookupContentType(new FileExtensionContentTypeProvider(), options, subPath, out var contentType);

        var context = new StaticFileContext(httpContext, options, NullLogger.Instance, fileProvider, contentType, subPath);

        var result = context.LookupFileInfo();
        Assert.True(validateResult);
        Assert.True(contentTypeResult);
        Assert.False(result);

        Assert.Equal(HttpsCompressionMode.Default, httpsCompressionFeature.Mode);
    }

    [Fact]
    public async Task RequestAborted_DoesntThrow()
    {
        var options = new StaticFileOptions();
        var fileProvider = new TestFileProvider();
        fileProvider.AddFile("/foo.txt", new TestFileInfo
        {
            LastModified = new DateTimeOffset(2014, 1, 2, 3, 4, 5, TimeSpan.Zero)
        });
        var pathString = new PathString("/test");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = new PathString("/test/foo.txt");
        httpContext.RequestAborted = new CancellationToken(canceled: true);
        var body = new MemoryStream();
        httpContext.Response.Body = body;
        var validateResult = StaticFileMiddleware.ValidatePath(httpContext, pathString, out var subPath);
        var contentTypeResult = StaticFileMiddleware.LookupContentType(new FileExtensionContentTypeProvider(), options, subPath, out var contentType);

        var context = new StaticFileContext(httpContext, options, NullLogger.Instance, fileProvider, contentType, subPath);

        var result = context.LookupFileInfo();
        Assert.True(validateResult);
        Assert.True(contentTypeResult);
        Assert.True(result);

        await context.SendAsync();

        Assert.Equal(0, body.Length);
    }

    private sealed class TestFileProvider : IFileProvider
    {
        private readonly Dictionary<string, IFileInfo> _files = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);

        public void AddFile(string path, IFileInfo fileInfo)
        {
            _files[path] = fileInfo;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (_files.TryGetValue(subpath, out var result))
            {
                return result;
            }

            return new NotFoundFileInfo();
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotSupportedException();
        }

        private class NotFoundFileInfo : IFileInfo
        {
            public bool Exists
            {
                get
                {
                    return false;
                }
            }

            public bool IsDirectory
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public DateTimeOffset LastModified
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string PhysicalPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }
    }

    private sealed class TestFileInfo : IFileInfo
    {
        public bool Exists
        {
            get { return true; }
        }

        public bool IsDirectory
        {
            get { return false; }
        }

        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public string Name { get; set; }

        public string PhysicalPath { get; set; }

        public Stream CreateReadStream()
        {
            return new MemoryStream();
        }
    }

    private class TestHttpsCompressionFeature : IHttpsCompressionFeature
    {
        public HttpsCompressionMode Mode { get; set; }
    }
}
