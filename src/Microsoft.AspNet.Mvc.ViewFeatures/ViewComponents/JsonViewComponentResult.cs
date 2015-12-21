// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// An <see cref="IViewComponentResult"/> which renders JSON text when executed.
    /// </summary>
    public class JsonViewComponentResult : IViewComponentResult
    {
        private readonly JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// Initializes a new <see cref="JsonViewComponentResult"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON text.</param>
        public JsonViewComponentResult(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new <see cref="JsonViewComponentResult"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON text.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        public JsonViewComponentResult(object value, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            Value = value;
            _serializerSettings = serializerSettings;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Renders JSON text to the output.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        public void Execute(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var serializerSettings = _serializerSettings;
            if (serializerSettings == null)
            {
                serializerSettings = context
                    .ViewContext
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IOptions<MvcJsonOptions>>()
                    .Value
                    .SerializerSettings;
            }

            using (var jsonWriter = new JsonTextWriter(context.Writer))
            {
                jsonWriter.CloseOutput = false;
                var jsonSerializer = JsonSerializer.Create(serializerSettings);
                jsonSerializer.Serialize(jsonWriter, Value);
            }
        }

        /// <summary>
        /// Renders JSON text to the output.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public Task ExecuteAsync(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Execute(context);
            return Task.FromResult(true);
        }
    }
}
