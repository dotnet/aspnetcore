// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class HeaderModelBinder : MetadataAwareBinder<IHeaderBinderMetadata>
    {
        /// <inheritdoc />
        protected override Task<bool> BindAsync(
            [NotNull] ModelBindingContext bindingContext, 
            [NotNull] IHeaderBinderMetadata metadata)
        {
            var request = bindingContext.OperationBindingContext.HttpContext.Request;

            if (bindingContext.ModelType == typeof(string))
            {
                var value = request.Headers.Get(bindingContext.ModelName);
                bindingContext.Model = value;

                return Task.FromResult(true);
            }
            else if (typeof(IEnumerable<string>).GetTypeInfo().IsAssignableFrom(
                bindingContext.ModelType.GetTypeInfo()))
            {
                var values = request.Headers.GetCommaSeparatedValues(bindingContext.ModelName);
                if (values != null)
                {
                    bindingContext.Model = ModelBindingHelper.ConvertValuesToCollectionType(bindingContext.ModelType, values);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}