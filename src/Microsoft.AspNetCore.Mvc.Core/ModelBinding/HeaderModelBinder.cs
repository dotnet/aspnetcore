// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NETSTANDARD1_3
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request headers when a model
    /// has the binding source <see cref="BindingSource.Header"/>/
    /// </summary>
    public class HeaderModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult or async state machines.

            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(BindingSource.Header))
            {
                // Headers are opt-in. This model either didn't specify [FromHeader] or specified something
                // incompatible so let other binders run.
                return TaskCache.CompletedTask;
            }

            var request = bindingContext.OperationBindingContext.HttpContext.Request;

            // Property name can be null if the model metadata represents a type (rather than a property or parameter).
            var headerName = bindingContext.FieldName;

            object model;
            if (ModelBindingHelper.CanGetCompatibleCollection<string>(bindingContext))
            {
                if (bindingContext.ModelType == typeof(string))
                {
                    var value = request.Headers[headerName];
                    model = (string)value;
                }
                else
                {
                    var values = request.Headers.GetCommaSeparatedValues(headerName);
                    model = GetCompatibleCollection(bindingContext, values);
                }
            }
            else
            {
                // An unsupported datatype or a new collection is needed (perhaps because target type is an array) but
                // can't assign it to the property.
                model = null;
            }

            if (model == null)
            {
                // Silently fail if unable to create an instance or use the current instance. Also reach here in the
                // typeof(string) case if the header does not exist in the request and in the
                // typeof(IEnumerable<string>) case if the header does not exist and this is not a top-level object.
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
            }
            else
            {
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    request.Headers.GetCommaSeparatedValues(headerName),
                    request.Headers[headerName]);

                bindingContext.Result = ModelBindingResult.Success(bindingContext.ModelName, model);
            }

            return TaskCache.CompletedTask;
        }

        private static object GetCompatibleCollection(ModelBindingContext bindingContext, string[] values)
        {
            // Almost-always success if IsTopLevelObject.
            if (!bindingContext.IsTopLevelObject && values.Length == 0)
            {
                return null;
            }

            if (bindingContext.ModelType.IsAssignableFrom(typeof(string[])))
            {
                // Array we already have is compatible.
                return values;
            }

            var collection = ModelBindingHelper.GetCompatibleCollection<string>(bindingContext, values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                collection.Add(values[i]);
            }

            return collection;
        }
    }
}