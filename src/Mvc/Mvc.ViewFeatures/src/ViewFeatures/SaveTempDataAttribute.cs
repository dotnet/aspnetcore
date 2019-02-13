// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A filter that saves the <see cref="ITempDataDictionary"/> for a request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SaveTempDataAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public SaveTempDataAttribute()
        {
            // Since SaveTempDataFilter registers for a response's OnStarting callback, we want this filter to run 
            // as early as possible to get the opportunity to register the call back before any other result filter 
            // starts writing to the response stream.
            Order = int.MinValue + 100;
        }

        // <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public bool IsReusable => true;

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<SaveTempDataFilter>();
        }
    }
}
