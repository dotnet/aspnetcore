// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;

namespace ModelBindingWebSite
{
    public class TestBindingSourceModelBinder : IModelBinder
    {
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(FromTestAttribute.TestBindingSource))
            {
                return ModelBindingResult.NoResultAsync;
            }

            var attributes = ((DefaultModelMetadata)bindingContext.ModelMetadata).Attributes;
            var metadata = attributes.Attributes.OfType<FromTestAttribute>().First();
            var model = metadata.Value;
            if (!IsSimpleType(bindingContext.ModelType))
            {
                model = Activator.CreateInstance(bindingContext.ModelType);
                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
            }

            return ModelBindingResult.FailedAsync(bindingContext.ModelName);
        }

        private bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                type.Equals(typeof(decimal)) ||
                type.Equals(typeof(string)) ||
                type.Equals(typeof(DateTime)) ||
                type.Equals(typeof(Guid)) ||
                type.Equals(typeof(DateTimeOffset)) ||
                type.Equals(typeof(TimeSpan));
        }
    }
}