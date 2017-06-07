// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
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

            var request = bindingContext.HttpContext.Request;

            // Property name can be null if the model metadata represents a type (rather than a property or parameter).
            var headerName = bindingContext.FieldName;

            object model;
            if (bindingContext.ModelType == typeof(string))
            {
                var value = request.Headers[headerName];
                model = (string)value;
            }
            else if (ModelBindingHelper.CanGetCompatibleCollection<string>(bindingContext))
            {
                var values = request.Headers.GetCommaSeparatedValues(headerName);
                model = GetCompatibleCollection(bindingContext, values);
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
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else
            {
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    request.Headers.GetCommaSeparatedValues(headerName),
                    request.Headers[headerName]);

                bindingContext.Result = ModelBindingResult.Success(model);
            }

            return Task.CompletedTask;
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