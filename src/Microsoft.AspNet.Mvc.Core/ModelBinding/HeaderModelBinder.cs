// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request headers when a model
    /// has the binding source <see cref="BindingSource.Header"/>/
    /// </summary>
    public class HeaderModelBinder : BindingSourceModelBinder
    {
        /// <summary>
        /// Creates a new <see cref="HeaderModelBinder"/>.
        /// </summary>
        public HeaderModelBinder()
            : base(BindingSource.Header)
        {
        }

        /// <inheritdoc />
        protected override Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
        {
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
                var validationNode = new ModelValidationNode(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata,
                    model);

                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    request.Headers.GetCommaSeparatedValues(headerName),
                    request.Headers[headerName]);

                return ModelBindingResult.SuccessAsync(bindingContext.ModelName, model, validationNode);
            }
        }
    }
}