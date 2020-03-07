// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal class ResponseCacheFilterApplicationModelProvider : IPageApplicationModelProvider
    {
        private readonly MvcOptions _mvcOptions;
        private readonly ILoggerFactory _loggerFactory;

        public ResponseCacheFilterApplicationModelProvider(IOptions<MvcOptions> mvcOptionsAccessor, ILoggerFactory loggerFactory)
        {
            if (mvcOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(mvcOptionsAccessor));
            }

            _mvcOptions = mvcOptionsAccessor.Value;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        // The order is set to execute after the DefaultPageApplicationModelProvider.
        public int Order => -1000 + 10;

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageModel = context.PageApplicationModel;
            var responseCacheAttributes = pageModel.HandlerTypeAttributes.OfType<ResponseCacheAttribute>();
            foreach (var attribute in responseCacheAttributes)
            {
                var cacheProfile = attribute.GetCacheProfile(_mvcOptions);
                context.PageApplicationModel.Filters.Add(new PageResponseCacheFilter(cacheProfile, _loggerFactory));
            }
        }

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }
    }
}
