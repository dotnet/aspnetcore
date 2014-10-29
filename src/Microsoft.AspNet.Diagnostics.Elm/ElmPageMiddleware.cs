// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Diagnostics.Elm.Views;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using System.Linq;

namespace Microsoft.AspNet.Diagnostics.Elm
{
    /// <summary>
    /// Enables viewing logs captured by the <see cref="ElmCaptureMiddleware"/>.
    /// </summary>
    public class ElmPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ElmOptions _options;
        private readonly ElmStore _store;

        public ElmPageMiddleware(RequestDelegate next, IOptions<ElmOptions> options, ElmStore store)
        {
            _next = next;
            _options = options.Options;
            _store = store;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_options.Path))
            {
                await _next(context);
                return;
            }

            var options = ParseParams(context);
            if (context.Request.Path == _options.Path)
            {
                RenderMainLogPage(options, context);
            }
            else
            {
                RenderRequestDetailsPage(options, context);
            }
        }

        private async void RenderMainLogPage(ViewOptions options, HttpContext context)
        {
            var model = new LogPageModel()
            {
                // sort so most recent logs are first
                Activities = _store.GetActivities(),
                Options = options,
                Path = _options.Path
            };
            var logPage = new LogPage(model);

            await logPage.ExecuteAsync(context);
        }

        private async void RenderRequestDetailsPage(ViewOptions options, HttpContext context)
        {
            var parts = context.Request.Path.Value.Split('/');
            var id = Guid.Empty;
            if (!Guid.TryParse(parts[parts.Length - 1], out id))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid Request Id");
                return;
            }
            var model = new RequestPageModel()
            {
                RequestID = id,
                Activity = _store.GetActivities().Where(a => a.HttpInfo?.RequestID == id).FirstOrDefault(),
                Options = options
            };
            var requestPage = new RequestPage(model);
            await requestPage.ExecuteAsync(context);
        }

        private ViewOptions ParseParams(HttpContext context)
        {
            var options = new ViewOptions()
            {
                MinLevel = LogLevel.Verbose,
                NamePrefix = string.Empty
            };
            if (context.Request.Query.ContainsKey("level"))
            {
                var minLevel = options.MinLevel;
                if (Enum.TryParse<LogLevel>(context.Request.Query["level"], out minLevel))
                {
                    options.MinLevel = minLevel;
                }
            }
            if (context.Request.Query.ContainsKey("name"))
            {
                var namePrefix = context.Request.Query.GetValues("name")[0];
                options.NamePrefix = namePrefix;
            }
            return options;
        }
    }
}