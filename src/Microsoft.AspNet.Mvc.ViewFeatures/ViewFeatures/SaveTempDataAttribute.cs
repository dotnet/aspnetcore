// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// Adds a filter which will save the <see cref="ITempDataDictionary"/> for a request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SaveTempDataAttribute : Attribute, IFilterFactory
    {
        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<SaveTempDataFilter>();
        }
    }
}
