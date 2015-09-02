// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request headers when a model
    /// has the binding source <see cref="BindingSource.Header"/>/
    /// </summary>
    public class HeaderModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(BindingSource.Header))
            {
                // Headers are opt-in. This model either didn't specify [FromHeader] or specified something
                // incompatible so let other binders run.
                return ModelBindingResult.NoResultAsync;
            }

            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            var modelMetadata = bindingContext.ModelMetadata;

            // Property name can be null if the model metadata represents a type (rather than a property or parameter).
            var headerName = bindingContext.FieldName;
            object model = null;
            if (bindingContext.ModelType == typeof(string))
            {
                string value = request.Headers[headerName];
                if (value != null)
                {
                    model = value;
                }
            }
            else if (typeof(IEnumerable<string>).IsAssignableFrom(bindingContext.ModelType))
            {
                var values = request.Headers.GetCommaSeparatedValues(headerName);
                if (values.Length > 0)
                {
                    model = ModelBindingHelper.ConvertValuesToCollectionType(
                        bindingContext.ModelType,
                        values);
                }
            }

            if (model == null)
            {
                return ModelBindingResult.FailedAsync(bindingContext.ModelName);
            }
            else
            {
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    request.Headers.GetCommaSeparatedValues(headerName),
                    request.Headers[headerName]);

                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model);
            }
        }
    }
}