// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    /// <summary>
    /// Executes a <see cref="JsonResult"/> to write to the response.
    /// </summary>
    internal class NewtonsoftJsonResultExecutor : IActionResultExecutor<JsonResult>
    {
        private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json")
        {
            Encoding = Encoding.UTF8
        }.ToString();

        private readonly IHttpResponseStreamWriterFactory _writerFactory;
        private readonly ILogger _logger;
        private readonly MvcOptions _mvcOptions;
        private readonly MvcNewtonsoftJsonOptions _jsonOptions;
        private readonly IArrayPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="NewtonsoftJsonResultExecutor"/>.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="logger">The <see cref="ILogger{NewtonsoftJsonResultExecutor}"/>.</param>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        /// <param name="jsonOptions">Accessor to <see cref="MvcNewtonsoftJsonOptions"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.</param>
        public NewtonsoftJsonResultExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            ILogger<NewtonsoftJsonResultExecutor> logger,
            IOptions<MvcOptions> mvcOptions,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
            ArrayPool<char> charPool)
        {
            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (jsonOptions == null)
            {
                throw new ArgumentNullException(nameof(jsonOptions));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            _writerFactory = writerFactory;
            _logger = logger;
            _mvcOptions = mvcOptions?.Value ?? throw new ArgumentNullException(nameof(mvcOptions));
            _jsonOptions = jsonOptions.Value;
            _charPool = new JsonArrayPool<char>(charPool);
        }

        /// <summary>
        /// Executes the <see cref="JsonResult"/> and writes the response.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="result">The <see cref="JsonResult"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete when writing has completed.</returns>
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

            var jsonSerializerSettings = GetSerializerSettings(result);

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

            _logger.JsonResultExecuting(result.Value);

            var responseStream = response.Body;
            FileBufferingWriteStream fileBufferingWriteStream = null;
            if (!_mvcOptions.SuppressOutputFormatterBuffering)
            {
                fileBufferingWriteStream = new FileBufferingWriteStream();
                responseStream = fileBufferingWriteStream;
            }

            try
            {
                await using (var writer = _writerFactory.CreateWriter(responseStream, resolvedContentTypeEncoding))
                {
                    using var jsonWriter = new JsonTextWriter(writer);
                    jsonWriter.ArrayPool = _charPool;
                    jsonWriter.CloseOutput = false;
                    jsonWriter.AutoCompleteOnClose = false;

                    var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
                    jsonSerializer.Serialize(jsonWriter, result.Value);
                }

                if (fileBufferingWriteStream != null)
                {
                    await fileBufferingWriteStream.DrainBufferAsync(response.Body);
                }
            }
            finally
            {
                if (fileBufferingWriteStream != null)
                {
                    await fileBufferingWriteStream.DisposeAsync();
                }
            }
        }

        private JsonSerializerSettings GetSerializerSettings(JsonResult result)
        {
            var serializerSettings = result.SerializerSettings;
            if (serializerSettings == null)
            {
                return _jsonOptions.SerializerSettings;
            }
            else
            {
                if (!(serializerSettings is JsonSerializerSettings settingsFromResult))
                {
                    throw new InvalidOperationException(Resources.FormatProperty_MustBeInstanceOfType(
                        nameof(JsonResult),
                        nameof(JsonResult.SerializerSettings),
                        typeof(JsonSerializerSettings)));
                }

                return settingsFromResult;
            }
        }
    }
}
