// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    public class ObjectResult : ActionResult, IStatusCodeActionResult
    {
        private MediaTypeCollection _contentTypes;

        public ObjectResult(object value)
        {
            Value = value;
            Formatters = new FormatterCollection<IOutputFormatter>();
            _contentTypes = new MediaTypeCollection();
        }

        [ActionResultObjectValue]
        public object Value { get; set; }

        public FormatterCollection<IOutputFormatter> Formatters { get; set; }

        public MediaTypeCollection ContentTypes
        {
            get => _contentTypes;
            set => _contentTypes = value ?? throw new ArgumentNullException(nameof(value));
        }

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

            if (Value is ProblemDetails details)
            {
                if (details.Status != null && StatusCode == null)
                {
                    StatusCode = details.Status;
                }
                else if (details.Status == null && StatusCode != null)
                {
                    details.Status = StatusCode;
                }
            }

            if (StatusCode.HasValue)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }
        }
    }
}