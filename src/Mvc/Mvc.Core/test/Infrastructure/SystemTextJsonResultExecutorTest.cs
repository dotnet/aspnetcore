// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class SystemTextJsonResultExecutorTest : JsonResultExecutorTestBase
    {
        protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
        {
            return new SystemTextJsonResultExecutor(
                Options.Create(new JsonOptions()),
                loggerFactory.CreateLogger<SystemTextJsonResultExecutor>());
        }

        [Fact]
        public async Task WriteResponseBodyAsync_WithNonUtf8Encoding_FormattingErrorsAreThrown()
        {
            // Arrange
            var context = GetActionContext();

            var result = new JsonResult(new ThrowingFormatterModel())
            {
                ContentType = "application/json; charset=utf-16",
            };
            var executor = CreateExecutor();

            // Act & Assert
            await Assert.ThrowsAsync<TimeZoneNotFoundException>(() => executor.ExecuteAsync(context, result));
        }

        [Fact]
        public async Task ExecuteAsync_AsyncEnumerableConnectionCloses()
        {
            var context = GetActionContext();
            var cts = new CancellationTokenSource();
            context.HttpContext.RequestAborted = cts.Token;
            var result = new JsonResult(AsyncEnumerableClosedConnection());
            var executor = CreateExecutor();
            var iterated = false;

            // Act
            await executor.ExecuteAsync(context, result);

            // Assert
            var written = GetWrittenBytes(context.HttpContext);
            // System.Text.Json might write the '[' before cancellation is observed
            Assert.InRange(written.Length, 0, 1);
            Assert.False(iterated);

            async IAsyncEnumerable<int> AsyncEnumerableClosedConnection([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                cts.Cancel();
                for (var i = 0; i < 100000 && !cancellationToken.IsCancellationRequested; i++)
                {
                    iterated = true;
                    yield return i;
                }
            }
        }

        protected override object GetIndentedSettings()
        {
            return new JsonSerializerOptions { WriteIndented = true };
        }

        [JsonConverter(typeof(ThrowingFormatterPersonConverter))]
        private class ThrowingFormatterModel
        {

        }

        private class ThrowingFormatterPersonConverter : JsonConverter<ThrowingFormatterModel>
        {
            public override ThrowingFormatterModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, ThrowingFormatterModel value, JsonSerializerOptions options)
            {
                throw new TimeZoneNotFoundException();
            }
        }
    }
}
