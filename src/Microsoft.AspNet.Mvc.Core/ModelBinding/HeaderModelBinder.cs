// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
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
            var headerName = bindingContext.BinderModelName ?? modelMetadata.PropertyName ?? bindingContext.ModelName;
            object model = null;
            if (bindingContext.ModelType == typeof(string))
            {
                var value = request.Headers.Get(headerName);
                if (value != null)
                {
                    model = value;
                }
            }
            else if (typeof(IEnumerable<string>).IsAssignableFrom(bindingContext.ModelType))
            {
                var values = request.Headers.GetCommaSeparatedValues(headerName);
                if (values != null)
                {
                    model = ModelBindingHelper.ConvertValuesToCollectionType(
                        bindingContext.ModelType,
                        values);
                }
            }

            ModelValidationNode validationNode = null;
            if (model != null)
            {
                validationNode = new ModelValidationNode(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata,
                    model);
                
                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName, 
                    request.Headers.GetCommaSeparatedValues(headerName).ToArray(), 
                    request.Headers.Get(headerName));
            }

            return Task.FromResult(
                new ModelBindingResult(
                    model,
                    bindingContext.ModelName,
                    isModelSet: model != null,
                    validationNode: validationNode));
        }
    }
}