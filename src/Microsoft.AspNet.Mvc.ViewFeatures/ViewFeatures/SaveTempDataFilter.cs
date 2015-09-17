// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// A filter which saves temp data.
    /// </summary>
    public class SaveTempDataFilter : IResourceFilter, IResultFilter
    {
        private readonly ITempDataDictionary _tempData;

        /// <summary>
        /// Creates a new instance of <see cref="SaveTempDataFilter"/>.
        /// </summary>
        /// <param name="tempData">The <see cref="ITempDataDictionary"/> for the current request.</param>
        public SaveTempDataFilter(ITempDataDictionary tempData)
        {
            _tempData = tempData;
        }

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            _tempData.Save();
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
                _tempData.Keep();
            }
        }
    }
}
