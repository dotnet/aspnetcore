// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    /// <summary>
    /// A filter that saves temp data.
    /// </summary>
    public class SaveTempDataFilter : IResourceFilter, IResultFilter
    {
        private readonly ITempDataDictionaryFactory _factory;

        /// <summary>
        /// Creates a new instance of <see cref="SaveTempDataFilter"/>.
        /// </summary>
        /// <param name="factory">The <see cref="ITempDataDictionaryFactory"/>.</param>
        public SaveTempDataFilter(ITempDataDictionaryFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            _factory.GetTempData(context.HttpContext).Save();
        }

        /// <inheritdoc />
        public void OnResultExecuting(ResultExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnResultExecuted(ResultExecutedContext context)
        {
            if (context.Result is IKeepTempDataResult)
            {
                _factory.GetTempData(context.HttpContext).Keep();
            }
        }
    }
}
