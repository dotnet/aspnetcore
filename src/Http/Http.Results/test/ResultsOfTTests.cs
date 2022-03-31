// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class ResultsOfTTests
{
    [Theory]
    [InlineData(0, typeof(OkObjectHttpResult))]
    [InlineData(1, typeof(NoContentHttpResult))]
    public void ResultsOfT_Result_IsAssignedResult(int input, Type expectedResult)
    {
        // Arrange
        Results<OkObjectHttpResult, NoContentHttpResult> MyApi(int id)
        {
            return id switch
            {
                0 => (OkObjectHttpResult)Results.Ok(),
                _ => (NoContentHttpResult)Results.NoContent()
            };
        }

        // Act
        var result = MyApi(input);

        // Assert
        Assert.IsType(expectedResult, result.Result);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(200, null)]
    public async Task ResultsOfT_ExecuteResult_ExecutesAssignedResult(int input, object expected)
    {
        // Arrange
        Results<CustomResult, NoContentHttpResult> MyApi(int checksum)
        {
            return checksum switch
            {
                100 => new CustomResult(checksum),
                _ => (NoContentHttpResult)Results.NoContent()
            };
        }
        var httpContext = GetHttpContext();

        // Act
        var result = MyApi(input);
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal(expected, httpContext.Items[result.Result]);
    }

    [Fact]
    public void ResultsOfT_Throws_InvalidOperationException_WhenResultIsNull()
    {
        // Arrange
        Results<CustomResult, NoContentHttpResult> MyApi()
        {
            return (CustomResult)null;
        }
        var httpContext = GetHttpContext();

        // Act & Assert
        var result = MyApi();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await result.ExecuteAsync(httpContext);
        });
    }

    class CustomResult : IResult
    {
        public CustomResult(int checksum)
        {
            Checksum = checksum;
        }

        public int Checksum { get; }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Items[this] = Checksum;
            return Task.CompletedTask;
        }
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
