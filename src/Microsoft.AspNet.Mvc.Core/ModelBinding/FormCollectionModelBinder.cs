// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Modelbinder to bind form values to <see cref="IFormCollection"/>.
    /// </summary>
    public class FormCollectionModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(IFormCollection) &&
                bindingContext.ModelType != typeof(FormCollection))
            {
                return null;
            }

            object model = null;
            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                if (bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(form.GetType().GetTypeInfo()))
                {
                    model = form;
                }
                else
                {
                    var formValuesLookup = form.ToDictionary(p => p.Key,
                                                             p => p.Value);
                    model = new FormCollection(formValuesLookup, form.Files);
                }
            }
            else
            {
                model = new FormCollection(new Dictionary<string, string[]>());
            }

            var validationNode =
                 new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, model);
            return new ModelBindingResult(
                model,
                bindingContext.ModelName,
                isModelSet: true,
                validationNode: validationNode);
        }
    }
}