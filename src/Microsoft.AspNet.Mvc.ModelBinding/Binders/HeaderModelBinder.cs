// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="MetadataAwareBinder{IHeaderBinderMetadata}"/> which uses <see cref="Http.HttpRequest.Headers"/>
    /// to bind the model.
    /// </summary>
    public class HeaderModelBinder : MetadataAwareBinder<IHeaderBinderMetadata>
    {
        /// <inheritdoc />
        protected override Task<bool> BindAsync(
            [NotNull] ModelBindingContext bindingContext,
            [NotNull] IHeaderBinderMetadata metadata)
        {
            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            var modelMetadata = bindingContext.ModelMetadata;

            // Property name can be null if the model metadata represents a type (rahter than a property or parameter).
            var headerName = modelMetadata.BinderModelName ?? modelMetadata.PropertyName ?? bindingContext.ModelName;
            if (bindingContext.ModelType == typeof(string))
            {
                var value = request.Headers.Get(headerName);
                if (value != null)
                {
                    bindingContext.Model = value;
                }
            }
            else if (typeof(IEnumerable<string>).GetTypeInfo().IsAssignableFrom(
                bindingContext.ModelType.GetTypeInfo()))
            {
                var values = request.Headers.GetCommaSeparatedValues(headerName);
                if (values != null)
                {
                    bindingContext.Model =
                        ModelBindingHelper.ConvertValuesToCollectionType(bindingContext.ModelType, values);
                }
            }

            // Always return true as header model binder is supposed to always handle IHeaderBinderMetadata.
            return Task.FromResult(true);
        }
    }
}