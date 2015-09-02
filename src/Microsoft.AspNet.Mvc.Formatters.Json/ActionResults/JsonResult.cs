// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.ActionResults
{
    /// <summary>
    /// An action result which formats the given object as JSON.
    /// </summary>
    public class JsonResult : ActionResult
    {
        private readonly JsonSerializerSettings _serializerSettings;

        private static readonly MediaTypeHeaderValue DefaultContentType = new MediaTypeHeaderValue("application/json")
        {
            Encoding = Encoding.UTF8
        };

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        public JsonResult(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        public JsonResult(object value, [NotNull] JsonSerializerSettings serializerSettings)
        {
            Value = value;
            _serializerSettings = serializerSettings;
        }

        /// <summary>
        /// Gets or sets the <see cref="MediaTypeHeaderValue"/> representing the Content-Type header of the response.
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the value to be formatted.
        /// </summary>
        public object Value { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;

            var contentTypeHeader = ContentType;
            if (contentTypeHeader == null)
            {
                contentTypeHeader = DefaultContentType;
            }
            else
            {
                if (contentTypeHeader.Encoding == null)
                {
                    // Do not modify the user supplied content type, so copy it instead
                    contentTypeHeader = contentTypeHeader.Copy();
                    contentTypeHeader.Encoding = Encoding.UTF8;
                }
            }

            response.ContentType = contentTypeHeader.ToString();

            if (StatusCode != null)
            {
                response.StatusCode = StatusCode.Value;
            }

            var serializerSettings = _serializerSettings;
            if (serializerSettings == null)
            {
                serializerSettings = context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IOptions<MvcJsonOptions>>()
                    .Value
                    .SerializerSettings;
            }

            using (var writer = new HttpResponseStreamWriter(response.Body, contentTypeHeader.Encoding))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.CloseOutput = false;
                    var jsonSerializer = JsonSerializer.Create(serializerSettings);
                    jsonSerializer.Serialize(jsonWriter, Value);
                }
            }

            return Task.FromResult(true);
        }
    }
}
