// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="IViewComponentResult"/> which renders JSON text when executed.
    /// </summary>
    public class JsonViewComponentResult : IViewComponentResult
    {
        /// <summary>
        /// Initializes a new <see cref="JsonViewComponentResult"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON text.</param>
        public JsonViewComponentResult(object value)
            : this(value, formatter: null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="JsonViewComponentResult"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON text.</param>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> to be used by
        /// the formatter.</param>
        public JsonViewComponentResult(object value, [NotNull] JsonSerializerSettings serializerSettings)
            : this(value, new JsonOutputFormatter(serializerSettings))
        {
        }

        /// <summary>
        /// Initializes a new <see cref="JsonViewComponentResult"/>.
        /// </summary>
        /// <param name="value">The value to format as JSON text.</param>
        /// <param name="formatter">The <see cref="JsonOutputFormatter"/> to use.</param>
        public JsonViewComponentResult(object value, JsonOutputFormatter formatter)
        {
            Value = value;
            Formatter = formatter;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets the formatter.
        /// </summary>
        public JsonOutputFormatter Formatter { get; }

        /// <summary>
        /// Renders JSON text to the output.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        public void Execute([NotNull] ViewComponentContext context)
        {
            var formatter = Formatter ?? ResolveFormatter(context);
            formatter.WriteObject(context.Writer, Value);
        }

        /// <summary>
        /// Renders JSON text to the output.
        /// </summary>
        /// <param name="context">The <see cref="ViewComponentContext"/>.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            Execute(context);
            return Task.FromResult(true);
        }

        private static JsonOutputFormatter ResolveFormatter(ViewComponentContext context)
        {
            var services = context.ViewContext.HttpContext.RequestServices;
            return services.GetRequiredService<JsonOutputFormatter>();
        }
    }
}
