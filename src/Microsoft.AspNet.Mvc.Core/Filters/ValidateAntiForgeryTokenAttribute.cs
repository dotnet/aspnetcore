// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public int Order { get; set; }

        public IFilter CreateInstance(IServiceProvider serviceProvider)
        {
            var antiForgery = serviceProvider.GetRequiredService<AntiForgery>();
            return new ValidateAntiForgeryTokenAuthorizationFilter(antiForgery);
        }
    }
}