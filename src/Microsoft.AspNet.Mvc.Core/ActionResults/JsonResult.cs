// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class JsonResult : ActionResult
    {
        private static readonly IList<MediaTypeHeaderValue> _defaultSupportedContentTypes =
                                                                new List<MediaTypeHeaderValue>()
                                                                {
                                                                    MediaTypeHeaderValue.Parse("application/json"),
                                                                    MediaTypeHeaderValue.Parse("text/json"),
                                                                };
        private IOutputFormatter _defaultFormatter;

        private ObjectResult _objectResult;

        public JsonResult(object data) :
            this(data, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="JsonResult"/> class.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="defaultFormatter">If no matching formatter is found, 
        /// the response is written to using defaultFormatter.</param>
        /// <remarks>
        /// The default formatter must be able to handle either application/json
        /// or text/json.
        /// </remarks>
        public JsonResult(object data, IOutputFormatter defaultFormatter)
        {
            _defaultFormatter = defaultFormatter;
            _objectResult = new ObjectResult(data);
        }

        public object Value
        {
            get
            {
                return _objectResult.Value;
            }
            set
            {
                _objectResult.Value = value;
            }
        }

        public IList<MediaTypeHeaderValue> ContentTypes
        {
            get
            {
                return _objectResult.ContentTypes;
            }
            set
            {
                _objectResult.ContentTypes = value;
            }
        }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            // Set the content type explicitly to application/json and text/json.
            // if the user has not already set it.
            if (ContentTypes == null || ContentTypes.Count == 0)
            {
                ContentTypes = _defaultSupportedContentTypes;
            }

            var formatterContext = new OutputFormatterContext()
            {
                DeclaredType = _objectResult.DeclaredType,
                ActionContext = context,
                Object = Value,
            };

            // Need to call this instead of directly calling _objectResult.ExecuteResultAsync
            // as that sets the status to 406 if a formatter is not found.
            // this can be cleaned up after https://github.com/aspnet/Mvc/issues/941 gets resolved.
            var formatter = SelectFormatter(formatterContext);
            await formatter.WriteAsync(formatterContext);
        }

        private IOutputFormatter SelectFormatter(OutputFormatterContext formatterContext)
        {
            var defaultFormatters = formatterContext.ActionContext
                                                    .HttpContext
                                                    .RequestServices
                                                    .GetRequiredService<IOutputFormattersProvider>()
                                                    .OutputFormatters;

            var formatter = _objectResult.SelectFormatter(formatterContext, defaultFormatters);
            if (formatter == null)
            {
                formatter = _defaultFormatter ?? formatterContext.ActionContext
                                                                 .HttpContext
                                                                 .RequestServices
                                                                 .GetRequiredService<JsonOutputFormatter>();
            }

            return formatter;
        }
    }
}
