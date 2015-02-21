// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An action result which formats the given object as JSON.
    /// </summary>
    public class JsonResult : ActionResult
    {
        /// <summary>
        /// The list of content-types used for formatting when <see cref="ContentTypes"/> is null or empty.
        /// </summary>
        public static readonly IReadOnlyList<MediaTypeHeaderValue> DefaultContentTypes = new MediaTypeHeaderValue[]
        {
            MediaTypeHeaderValue.Parse("application/json"),
            MediaTypeHeaderValue.Parse("text/json"),
        };

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="data"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        public JsonResult(object value)
            : this(value, formatter: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JsonResult"/> with the given <paramref name="data"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON.</param>
        /// <param name="formatter">The formatter to use, or <c>null</c> to choose a formatter dynamically.</param>
        public JsonResult(object value, IOutputFormatter formatter)
        {
            Value = value;
            Formatter = formatter;

            ContentTypes = new List<MediaTypeHeaderValue>();
        }

        /// <summary>
        /// Gets or sets the list of supported Content-Types.
        /// </summary>
        public IList<MediaTypeHeaderValue> ContentTypes { get; set; }

        /// <summary>
        /// Gets or sets the formatter.
        /// </summary>
        public IOutputFormatter Formatter { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the value to be formatted.
        /// </summary>
        public object Value { get; set; }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var objectResult = new ObjectResult(Value);

            // Set the content type explicitly to application/json and text/json.
            // if the user has not already set it.
            if (ContentTypes == null || ContentTypes.Count == 0)
            {
                foreach (var contentType in DefaultContentTypes)
                {
                    objectResult.ContentTypes.Add(contentType);
                }
            }
            else
            {
                objectResult.ContentTypes = ContentTypes;
            }

            var formatterContext = new OutputFormatterContext()
            {
                ActionContext = context,
                DeclaredType = objectResult.DeclaredType,
                Object = Value,
            };

            // JsonResult expects to always find a formatter, in contrast with ObjectResult, which might return
            // a 406.
            var formatter = SelectFormatter(objectResult, formatterContext);
            Debug.Assert(formatter != null);

            if (StatusCode != null)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }

            await formatter.WriteAsync(formatterContext);
        }

        private IOutputFormatter SelectFormatter(ObjectResult objectResult, OutputFormatterContext formatterContext)
        {
            if (Formatter == null)
            {
                // If no formatter was provided, then run Conneg with the formatters configured in options.
                var formatters = formatterContext
                    .ActionContext
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<IOutputFormattersProvider>()
                    .OutputFormatters
                    .OfType<IJsonOutputFormatter>()
                    .ToArray();

                var formatter = objectResult.SelectFormatter(formatterContext, formatters);
                if (formatter == null)
                {
                    // If the available user-configured formatters can't write this type, then fall back to the
                    // 'global' one.
                    formatter = formatterContext
                        .ActionContext
                        .HttpContext
                        .RequestServices
                        .GetRequiredService<JsonOutputFormatter>();

                    // Run SelectFormatter again to try to choose a content type that this formatter can do.
                    objectResult.SelectFormatter(formatterContext, new[] { formatter });
                }

                return formatter;
            }
            else
            {
                // Run SelectFormatter to try to choose a content type that this formatter can do.
                objectResult.SelectFormatter(formatterContext, new[] { Formatter });
                return Formatter;
            }
        }
    }
}
