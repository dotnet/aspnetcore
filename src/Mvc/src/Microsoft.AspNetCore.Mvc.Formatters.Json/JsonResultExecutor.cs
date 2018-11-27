// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Executes a <see cref="JsonResult"/> to write to the response.
    /// </summary>
    internal class JsonResultExecutor
    {
        private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json")
        {
            Encoding = Encoding.UTF8
        }.ToString();

        private readonly IHttpResponseStreamWriterFactory _writerFactory;
        private readonly ILogger _logger;
        private readonly MvcJsonOptions _options;
        private readonly IArrayPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="JsonResultExecutor"/>.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="logger">The <see cref="ILogger{JsonResultExecutor}"/>.</param>
        /// <param name="options">The <see cref="IOptions{MvcJsonOptions}"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.</param>
        public JsonResultExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            ILogger<JsonResultExecutor> logger,
            IOptions<MvcJsonOptions> options,
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

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            _writerFactory = writerFactory;
            _logger = logger;
            _options = options.Value;
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

            var serializerSettings = result.SerializerSettings ?? _options.SerializerSettings;

            _logger.JsonResultExecuting(result.Value);
            using (var writer = _writerFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.ArrayPool = _charPool;
                    jsonWriter.CloseOutput = false;
                    jsonWriter.AutoCompleteOnClose = false;

                    var jsonSerializer = JsonSerializer.Create(serializerSettings);
                    jsonSerializer.Serialize(jsonWriter, result.Value);
                }

                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous write).
                await writer.FlushAsync();
            }
        }
    }
}
