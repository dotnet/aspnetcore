// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
                    bindingContext.Model = ConvertValuesToCollectionType(bindingContext.ModelType, values);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private object ConvertValuesToCollectionType(Type modelType, IList<string> values)
        {
            // There's a limited set of collection types we can support here.
            //
            // For the simple cases - choose a string[] or List<string> if the destination type supports
            // it.
            // 
            // For more complex cases, if the destination type is a class and implements ICollection<string>
            // then activate it and add the values.
            //
            // Otherwise just give up.
            if (typeof(List<string>).IsAssignableFrom(modelType))
            {
                return new List<string>(values);
            }
            else if (typeof(string[]).IsAssignableFrom(modelType))
            {
                return values.ToArray();
            }
            else if (
                modelType.GetTypeInfo().IsClass && 
                !modelType.GetTypeInfo().IsAbstract &&
                typeof(ICollection<string>).IsAssignableFrom(modelType))
            {
                var result = (ICollection<string>)Activator.CreateInstance(modelType);
                foreach (var value in values)
                {
                    result.Add(value);
                }

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}