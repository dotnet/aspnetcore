// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

public class FileResultTest
{
    [Fact]
    public void Constructor_SetsContentType()
    {
        // Act
        var result = new EmptyFileResult("text/plain");

        // Assert
        Assert.Equal("text/plain", result.ContentType.ToString());
    }

    [Fact]
    public async Task ContentDispositionHeader_IsEncodedCorrectly()
    {
        // See comment in FileResult.cs detailing how the FileDownloadName should be encoded.

        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = CreateActionContext(httpContext);

        var result = new EmptyFileResult("application/my-type")
        {
            FileDownloadName = @"some\file"
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.True(result.WasWriteFileCalled);

        Assert.Equal("application/my-type", httpContext.Response.Headers["Content-Type"]);
        Assert.Equal(@"attachment; filename=""some\\file""; filename*=UTF-8''some%5Cfile", httpContext.Response.Headers["Content-Disposition"]);
    }

    [Fact]
    public async Task ContentDispositionHeader_IsEncodedCorrectly_ForUnicodeCharacters()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = CreateActionContext(httpContext);

        var result = new EmptyFileResult("application/my-type")
        {
            FileDownloadName = "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ"
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.True(result.WasWriteFileCalled);
        Assert.Equal("application/my-type", httpContext.Response.Headers["Content-Type"]);
        Assert.Equal(@"attachment; filename=""ABCXYZabcxyz012789!@#$%^&*()-=_+.:~_""; filename*=UTF-8''ABCXYZabcxyz012789!%40#$%25^&%2A%28%29-%3D_+.%3A~%CE%94",
            httpContext.Response.Headers["Content-Disposition"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_DoesNotSetContentDisposition_IfNotSpecified()
    {
        // Arrange
        var provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<EmptyFileResultExecutor>()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = provider;

        var actionContext = CreateActionContext(httpContext);

        var result = new EmptyFileResult("application/my-type");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.True(result.WasWriteFileCalled);
        Assert.Equal("application/my-type", httpContext.Response.ContentType);
        Assert.Equal(Stream.Null, httpContext.Response.Body);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsContentDisposition_IfSpecified()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = CreateActionContext(httpContext);

        var result = new EmptyFileResult("application/my-type")
        {
            FileDownloadName = "filename.ext"
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.True(result.WasWriteFileCalled);
        Assert.Equal("application/my-type", httpContext.Response.ContentType);
        Assert.Equal("attachment; filename=filename.ext; filename*=UTF-8''filename.ext", httpContext.Response.Headers["Content-Disposition"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_ThrowsException_IfCannotResolveLoggerFactory()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        var actionContext = CreateActionContext(httpContext);
        var result = new EmptyFileResult("application/my-type");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteResultAsync(actionContext));
    }

    public static TheoryData<string, string> ContentDispositionData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    // Non quoted values
                    { "09aAzZ", "attachment; filename=09aAzZ; filename*=UTF-8''09aAzZ" },
                    { "a.b", "attachment; filename=a.b; filename*=UTF-8''a.b" },
                    { "#", "attachment; filename=#; filename*=UTF-8''#" },
                    { "-", "attachment; filename=-; filename*=UTF-8''-" },
                    { "_", "attachment; filename=_; filename*=UTF-8''_" },
                    { "~", "attachment; filename=~; filename*=UTF-8''~" },
                    { "$", "attachment; filename=$; filename*=UTF-8''$" },
                    { "&", "attachment; filename=&; filename*=UTF-8''&" },
                    { "+", "attachment; filename=+; filename*=UTF-8''+" },
                    { "!", "attachment; filename=!; filename*=UTF-8''!" },
                    { "^", "attachment; filename=^; filename*=UTF-8''^" },
                    { "`", "attachment; filename=`; filename*=UTF-8''`" },
                    { "|", "attachment; filename=|; filename*=UTF-8''|" },

                    // Values that need to be quoted
                    { ": :", "attachment; filename=\": :\"; filename*=UTF-8''%3A%20%3A" },
                    { "(", "attachment; filename=\"(\"; filename*=UTF-8''%28" },
                    { ")", "attachment; filename=\")\"; filename*=UTF-8''%29" },
                    { "<", "attachment; filename=\"<\"; filename*=UTF-8''%3C" },
                    { ">", "attachment; filename=\">\"; filename*=UTF-8''%3E" },
                    { "@", "attachment; filename=\"@\"; filename*=UTF-8''%40" },
                    { ",", "attachment; filename=\",\"; filename*=UTF-8''%2C" },
                    { ";", "attachment; filename=\";\"; filename*=UTF-8''%3B" },
                    { ":", "attachment; filename=\":\"; filename*=UTF-8''%3A" },
                    { "/", "attachment; filename=\"/\"; filename*=UTF-8''%2F" },
                    { "[", "attachment; filename=\"[\"; filename*=UTF-8''%5B" },
                    { "]", "attachment; filename=\"]\"; filename*=UTF-8''%5D" },
                    { "?", "attachment; filename=\"?\"; filename*=UTF-8''%3F" },
                    { "=", "attachment; filename=\"=\"; filename*=UTF-8''%3D" },
                    { "{", "attachment; filename=\"{\"; filename*=UTF-8''%7B" },
                    { "}", "attachment; filename=\"}\"; filename*=UTF-8''%7D" },
                    { " ", "attachment; filename=\" \"; filename*=UTF-8''%20" },
                    { "a b", "attachment; filename=\"a b\"; filename*=UTF-8''a%20b" },

                    // Values that need to be escaped
                    { "\"", "attachment; filename=\"\\\"\"; filename*=UTF-8''%22" },
                    { "\\", "attachment; filename=\"\\\\\"; filename*=UTF-8''%5C" },

                    // Values that need to be specially encoded (Base64, see rfc2047)
                    { "a\tb", "attachment; filename=a_b; filename*=UTF-8''a%09b" },
                    { "a\nb", "attachment; filename=a_b; filename*=UTF-8''a%0Ab" },

                    // Values with non unicode characters
                    { "résumé.txt", "attachment; filename=r_sum_.txt; filename*=UTF-8''r%C3%A9sum%C3%A9.txt" },
                    { "Δ", "attachment; filename=_; filename*=UTF-8''%CE%94" },
                    { "Δ\t", "attachment; filename=__; filename*=UTF-8''%CE%94%09" },
                    { "ABCXYZabcxyz012789!@#$%^&*()-=_+.:~Δ", @"attachment; filename=""ABCXYZabcxyz012789!@#$%^&*()-=_+.:~_""; filename*=UTF-8''ABCXYZabcxyz012789!%40#$%25^&%2A%28%29-%3D_+.%3A~%CE%94" },
                };
        }
    }

    public static TheoryData<string, string> ContentDispositionControlCharactersData
    {
        get
        {
            var data = new TheoryData<string, string>();
            for (var i = 0; i < 32; i++)
            {
                data.Add(char.ConvertFromUtf32(i), $"attachment; filename=_; filename*=UTF-8''%{i:X2}");
            }

            data.Add(char.ConvertFromUtf32(127), $"attachment; filename=_; filename*=UTF-8''%7F");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ContentDispositionData))]
    [MemberData(nameof(ContentDispositionControlCharactersData))]
    public void GetHeaderValue_Produces_Correct_ContentDisposition(string input, string expectedOutput)
    {
        // Arrange & Act
        var cd = new ContentDispositionHeaderValue("attachment");
        cd.SetHttpFileName(input);
        var actual = cd.ToString();

        // Assert
        Assert.Equal(expectedOutput, actual);
    }

    [Fact]
    public async Task SetsAcceptRangeHeader()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = CreateActionContext(httpContext);

        var result = new EmptyFileResult("application/my-type");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal("bytes", httpContext.Response.Headers.AcceptRanges);
    }

    [Theory]
    [InlineData("\"Etag\"", "\"NotEtag\"", true, "\"Etag\"")]
    [InlineData("\"Etag\"", "\"NotEtag\"", false, "\"Etag\"")]
    [InlineData("\"Etag\"", null, false, null)]
    [InlineData(null, "\"NotEtag\"", true, "\"Etag\"")]
    [InlineData(null, "\"NotEtag\"", false, "\"Etag\"")]
    public void GetPreconditionState_ShouldProcess(string ifMatch, string ifNoneMatch, bool isWeak, string ifRange)
    {
        // Arrange
        var actionContext = new ActionContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
        var lastModified = DateTimeOffset.MinValue;
        lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
        var etag = new EntityTagHeaderValue("\"Etag\"");
        httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifMatch),
            };

        httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifNoneMatch, isWeak),
            };
        httpRequestHeaders.IfRange = ifRange == null ? null : new RangeConditionHeaderValue(ifRange);
        httpRequestHeaders.IfUnmodifiedSince = lastModified;
        httpRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(1);
        actionContext.HttpContext = httpContext;

        // Act
        var state = FileResultHelper.GetPreconditionState(
            httpRequestHeaders,
            lastModified,
            etag,
            NullLogger.Instance);

        // Assert
        Assert.Equal(FileResultHelper.PreconditionState.ShouldProcess, state);
    }

    [Theory]
    [InlineData("\"NotEtag\"", null, true)]
    [InlineData("\"NotEtag\"", null, false)]
    [InlineData("\"Etag\"", "\"Etag\"", true)]
    [InlineData("\"Etag\"", "\"Etag\"", false)]
    [InlineData(null, null, false)]
    public void GetPreconditionState_ShouldNotProcess_PreconditionFailed(string ifMatch, string ifNoneMatch, bool isWeak)
    {
        // Arrange
        var actionContext = new ActionContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Delete;
        var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
        var lastModified = DateTimeOffset.MinValue.AddDays(1);
        var etag = new EntityTagHeaderValue("\"Etag\"");
        httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifMatch),
            };

        httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifNoneMatch, isWeak),
            };
        httpRequestHeaders.IfUnmodifiedSince = DateTimeOffset.MinValue;
        httpRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue.AddDays(2);
        actionContext.HttpContext = httpContext;

        // Act
        var state = FileResultHelper.GetPreconditionState(
            httpRequestHeaders,
            lastModified,
            etag,
            NullLogger.Instance);

        // Assert
        Assert.Equal(FileResultHelper.PreconditionState.PreconditionFailed, state);
    }

    [Theory]
    [InlineData(null, "\"Etag\"", true)]
    [InlineData(null, "\"Etag\"", false)]
    [InlineData(null, null, false)]
    public void GetPreconditionState_ShouldNotProcess_NotModified(string ifMatch, string ifNoneMatch, bool isWeak)
    {
        // Arrange
        var actionContext = new ActionContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
        var lastModified = DateTimeOffset.MinValue;
        lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
        var etag = new EntityTagHeaderValue("\"Etag\"");
        httpRequestHeaders.IfMatch = ifMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifMatch),
            };

        httpRequestHeaders.IfNoneMatch = ifNoneMatch == null ? null : new[]
        {
                new EntityTagHeaderValue(ifNoneMatch, isWeak),
            };
        httpRequestHeaders.IfModifiedSince = lastModified;
        actionContext.HttpContext = httpContext;

        // Act
        var state = FileResultHelper.GetPreconditionState(
            httpRequestHeaders,
            lastModified,
            etag,
            NullLogger.Instance);

        // Assert
        Assert.Equal(FileResultHelper.PreconditionState.NotModified, state);
    }

    [Theory]
    [InlineData("\"NotEtag\"", false)]
    [InlineData("\"Etag\"", true)]
    public void IfRangeValid_IgnoreRangeRequest(string ifRangeString, bool expected)
    {
        // Arrange
        var actionContext = new ActionContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        var httpRequestHeaders = httpContext.Request.GetTypedHeaders();
        var lastModified = DateTimeOffset.MinValue;
        lastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0));
        var etag = new EntityTagHeaderValue("\"Etag\"");
        httpRequestHeaders.IfRange = new RangeConditionHeaderValue(ifRangeString);
        httpRequestHeaders.IfModifiedSince = lastModified;
        actionContext.HttpContext = httpContext;

        // Act
        var ifRangeIsValid = FileResultHelper.IfRangeValid(
            httpRequestHeaders,
            lastModified,
            etag,
            NullLogger.Instance);

        // Assert
        Assert.Equal(expected, ifRangeIsValid);
    }

    public static TheoryData<DateTimeOffset, int> LastModifiedDateData
    {
        get
        {
            return new TheoryData<DateTimeOffset, int>()
                {
                    { new DateTimeOffset(2018, 4, 9, 11, 23, 22, TimeSpan.Zero), 200 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 21, TimeSpan.Zero), 200 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 22, TimeSpan.Zero), 304 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 23, TimeSpan.Zero), 304 },
                    { new DateTimeOffset(2018, 4, 9, 11, 25, 22, TimeSpan.Zero), 304 },
                };
        }
    }

    [Theory]
    [MemberData(nameof(LastModifiedDateData))]
    public async Task IfModifiedSinceComparison_OnlyUsesWholeSeconds(
        DateTimeOffset ifModifiedSince,
        int expectedStatusCode)
    {
        // Arrange
        var httpContext = GetHttpContext();
        httpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(ifModifiedSince);
        var actionContext = CreateActionContext(httpContext);
        // Represents 4/9/2018 11:24:22 AM +00:00
        // Ticks rounded down to seconds: 636588698620000000
        var ticks = 636588698625969382;
        var result = new EmptyFileResult("application/test")
        {
            LastModified = new DateTimeOffset(ticks, TimeSpan.Zero)
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }

    public static TheoryData<DateTimeOffset, int> IfUnmodifiedSinceDateData
    {
        get
        {
            return new TheoryData<DateTimeOffset, int>()
                {
                    { new DateTimeOffset(2018, 4, 9, 11, 23, 22, TimeSpan.Zero), 412 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 21, TimeSpan.Zero), 412 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 22, TimeSpan.Zero), 200 },
                    { new DateTimeOffset(2018, 4, 9, 11, 24, 23, TimeSpan.Zero), 200 },
                    { new DateTimeOffset(2018, 4, 9, 11, 25, 22, TimeSpan.Zero), 200 },
                };
        }
    }

    [Theory]
    [MemberData(nameof(IfUnmodifiedSinceDateData))]
    public async Task IfUnmodifiedSinceComparison_OnlyUsesWholeSeconds(DateTimeOffset ifUnmodifiedSince, int expectedStatusCode)
    {
        // Arrange
        var httpContext = GetHttpContext();
        httpContext.Request.Headers.IfUnmodifiedSince = HeaderUtilities.FormatDate(ifUnmodifiedSince);
        var actionContext = CreateActionContext(httpContext);
        // Represents 4/9/2018 11:24:22 AM +00:00
        // Ticks rounded down to seconds: 636588698620000000
        var ticks = 636588698625969382;
        var result = new EmptyFileResult("application/test")
        {
            LastModified = new DateTimeOffset(ticks, TimeSpan.Zero)
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EmptyFileResultExecutor>();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static ActionContext CreateActionContext(HttpContext context)
    {
        return new ActionContext(context, new RouteData(), new ActionDescriptor());
    }

    private class EmptyFileResult : FileResult
    {
        public bool WasWriteFileCalled;

        public EmptyFileResult()
            : base("application/octet")
        {
        }

        public EmptyFileResult(string contentType)
            : base(contentType)
        {
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            var executor = context.HttpContext.RequestServices.GetRequiredService<EmptyFileResultExecutor>();
            return executor.ExecuteAsync(context, this);
        }
    }

    private class EmptyFileResultExecutor : FileResultExecutorBase
    {
        public EmptyFileResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<EmptyFileResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, EmptyFileResult result)
        {
            SetHeadersAndLog(
                context,
                result,
                fileLength: 0L,
                enableRangeProcessing: true,
                lastModified: result.LastModified);
            result.WasWriteFileCalled = true;
            return Task.FromResult(0);
        }
    }
}
