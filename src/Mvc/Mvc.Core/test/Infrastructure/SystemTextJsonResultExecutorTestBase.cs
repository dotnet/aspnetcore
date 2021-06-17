// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public abstract class SystemTextJsonResultExecutorTestBase : JsonResultExecutorTestBase
    {
        protected override IActionResultExecutor<JsonResult> CreateExecutor(ILoggerFactory loggerFactory)
        {
            return new SystemTextJsonResultExecutor(
                Options.Create(new JsonOptions()), 
                loggerFactory.CreateLogger<SystemTextJsonResultExecutor>(),
                Options.Create(new MvcOptions()));
        }

        [Fact]
        public async Task WriteResponseBodyAsync_WithThrowingJsonConverter_Throws()
        {
            // Arrange
            var context = GetActionContext();

            var result = new JsonResult(new ThrowingFormatterModel());

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
}
