// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FiltersWebSite
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var formValueProviderFactory = context.ValueProviderFactories
                .OfType<FormValueProviderFactory>()
                .FirstOrDefault();
            if (formValueProviderFactory != null)
            {
                context.ValueProviderFactories.Remove(formValueProviderFactory);
            }

            var jqueryFormValueProviderFactory = context.ValueProviderFactories
                .OfType<JQueryFormValueProviderFactory>()
                .FirstOrDefault();
            if (jqueryFormValueProviderFactory != null)
            {
                context.ValueProviderFactories.Remove(jqueryFormValueProviderFactory);
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
