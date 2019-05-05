// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal sealed class SystemTextJsonResultExecutor : IActionResultExecutor<JsonResult>
    {
        private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json")
        {
            Encoding = Encoding.UTF8
        }.ToString();

        private readonly MvcOptions _mvcOptions;
        private readonly ILogger<SystemTextJsonResultExecutor> _logger;

        public SystemTextJsonResultExecutor(
            IOptions<MvcOptions> mvcOptions,
            ILogger<SystemTextJsonResultExecutor> logger)
        {
            _mvcOptions = mvcOptions.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(ActionContext context, JsonResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var jsonSerializerOptions = GetSerializerOptions(result);

            var response = context.HttpContext.Response;

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(
                result.ContentType,
                response.ContentType,
                DefaultContentType,
                out var resolvedContentType,
                out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            Log.JsonResultExecuting(_logger, result.Value);

            // Keep this code in sync with SystemTextJsonOutputFormatter
            var writeStream = GetWriteStream(context.HttpContext, resolvedContentTypeEncoding);
            try
            {
                var type = result.Value?.GetType() ?? typeof(object);
                await JsonSerializer.WriteAsync(result.Value, type, writeStream, jsonSerializerOptions);
                await writeStream.FlushAsync();
            }
            finally
            {
                if (writeStream is TranscodingWriteStream transcoding)
                {
                    await transcoding.DisposeAsync();
                }
            }
        }

        private Stream GetWriteStream(HttpContext httpContext, Encoding selectedEncoding)
        {
            if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
            {
                // JsonSerializer does not write a BOM. Therefore we do not have to handle it
                // in any special way.
                return httpContext.Response.Body;
            }

            return new TranscodingWriteStream(httpContext.Response.Body, selectedEncoding);
        }

        private JsonSerializerOptions GetSerializerOptions(JsonResult result)
        {
            var serializerSettings = result.SerializerSettings;
            if (serializerSettings == null)
            {
                return _mvcOptions.SerializerOptions;
            }
            else
            {
                if (!(serializerSettings is JsonSerializerOptions settingsFromResult))
                {
                    throw new InvalidOperationException(Resources.FormatProperty_MustBeInstanceOfType(
                        nameof(JsonResult),
                        nameof(JsonResult.SerializerSettings),
                        typeof(JsonSerializerOptions)));
                }

                return settingsFromResult;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _jsonResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, "JsonResultExecuting"),
                "Executing JsonResult, writing value of type '{Type}'.");

            public static void JsonResultExecuting(ILogger logger, object value)
            {
                var type = value == null ? "null" : value.GetType().FullName;
                _jsonResultExecuting(logger, type, null);
            }
        }
    }
}
