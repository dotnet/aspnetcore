// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        private readonly JsonOptions _options;
        private readonly ILogger<SystemTextJsonResultExecutor> _logger;
        private readonly AsyncEnumerableReader _asyncEnumerableReaderFactory;

        public SystemTextJsonResultExecutor(
            IOptions<JsonOptions> options,
            ILogger<SystemTextJsonResultExecutor> logger,
            IOptions<MvcOptions> mvcOptions)
        {
            _options = options.Value;
            _logger = logger;
            _asyncEnumerableReaderFactory = new AsyncEnumerableReader(mvcOptions.Value);
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
                var value = result.Value;
                if (value != null && _asyncEnumerableReaderFactory.TryGetReader(value.GetType(), out var reader))
                {
                    Log.BufferingAsyncEnumerable(_logger, value);
                    value = await reader(value);
                }

                var type = value?.GetType() ?? typeof(object);
                await JsonSerializer.SerializeAsync(writeStream, value, type, jsonSerializerOptions);

                // The transcoding streams use Encoders and Decoders that have internal buffers. We need to flush these
                // when there is no more data to be written. Stream.FlushAsync isn't suitable since it's
                // acceptable to Flush a Stream (multiple times) prior to completion.
                if (writeStream is TranscodingWriteStream transcodingStream)
                {
                    await transcodingStream.FinalWriteAsync(CancellationToken.None);
                }
                await writeStream.FlushAsync();
            }
            finally
            {
                if (writeStream is TranscodingWriteStream transcodingStream)
                {
                    await transcodingStream.DisposeAsync();
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
                return _options.JsonSerializerOptions;
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

            private static readonly Action<ILogger, string, Exception> _bufferingAsyncEnumerable = LoggerMessage.Define<string>(
               LogLevel.Debug,
               new EventId(2, "BufferingAsyncEnumerable"),
               "Buffering IAsyncEnumerable instance of type '{Type}'.");

            public static void JsonResultExecuting(ILogger logger, object value)
            {
                var type = value == null ? "null" : value.GetType().FullName;
                _jsonResultExecuting(logger, type, null);
            }

            public static void BufferingAsyncEnumerable(ILogger logger, object asyncEnumerable)
                => _bufferingAsyncEnumerable(logger, asyncEnumerable.GetType().FullName, null);
        }
    }
}
