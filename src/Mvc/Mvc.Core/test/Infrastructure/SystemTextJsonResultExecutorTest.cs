// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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
