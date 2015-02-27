// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;

namespace ModelBindingWebSite
{
    public class TestBindingSourceModelBinder : BindingSourceModelBinder
    {
        public TestBindingSourceModelBinder()
            : base(FromTestAttribute.TestBindingSource)
        {
        }

        protected override Task<ModelBindingResult> BindModelCoreAsync(ModelBindingContext bindingContext)
        {
            var attributes = ((DefaultModelMetadata)bindingContext.ModelMetadata).Attributes;
            var metadata = attributes.OfType<FromTestAttribute>().First();
            var model = metadata.Value;
            if (!IsSimpleType(bindingContext.ModelType))
            {
                model = Activator.CreateInstance(bindingContext.ModelType);
                return Task.FromResult(new ModelBindingResult(model, bindingContext.ModelName, true));
            }

            return Task.FromResult(new ModelBindingResult(null, bindingContext.ModelName, false));
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