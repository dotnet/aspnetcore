// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    public class ObjectResult : ActionResult
    {
        public ObjectResult(object value)
        {
            Value = value;
            Formatters = new FormatterCollection<IOutputFormatter>();
            ContentTypes = new MediaTypeCollection();
        }

        public object Value { get; set; }

        public FormatterCollection<IOutputFormatter> Formatters { get; set; }

        public MediaTypeCollection ContentTypes { get; set; }

        public Type DeclaredType { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int? StatusCode { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ObjectResult>>();
            return executor.ExecuteAsync(context, this);
        }

        /// <summary>
        /// This method is called before the formatter writes to the output stream.
        /// </summary>
        public virtual void OnFormatting(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (StatusCode.HasValue)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }
        }
    }
}