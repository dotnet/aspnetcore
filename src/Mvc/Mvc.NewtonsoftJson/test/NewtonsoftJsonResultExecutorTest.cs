// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

public class NewtonsoftJsonResultExecutorTest : JsonResultExecutorTestBase
{
    protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
    {
        return new NewtonsoftJsonResultExecutor(
            new TestHttpResponseStreamWriterFactory(),
            loggerFactory.CreateLogger<NewtonsoftJsonResultExecutor>(),
            Options.Create(new MvcOptions()),
            Options.Create(new MvcNewtonsoftJsonOptions()),
            ArrayPool<char>.Shared);
    }

    protected override object GetIndentedSettings()
    {
        return new JsonSerializerSettings { Formatting = Formatting.Indented };
    }

    [Fact]
    public async Task ExecuteAsync_AsyncEnumerableReceivesCancellationToken()
    {
        // Arrange
        var expected = System.Text.Json.JsonSerializer.Serialize(new[] { "Hello", "world" });

        var cts = new CancellationTokenSource();
        var context = GetActionContext();
        context.HttpContext.RequestAborted = cts.Token;
        var result = new JsonResult(TestAsyncEnumerable());
        var executor = CreateExecutor();
        CancellationToken token = default;

        // Act
        await executor.ExecuteAsync(context, result);

        // Assert
        var written = GetWrittenBytes(context.HttpContext);
        Assert.Equal(expected, Encoding.UTF8.GetString(written));

        cts.Cancel();
        Assert.Equal(cts.Token, token);

        async IAsyncEnumerable<string> TestAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            token = cancellationToken;
            yield return "Hello";
            yield return "world";
        }
    }
}
