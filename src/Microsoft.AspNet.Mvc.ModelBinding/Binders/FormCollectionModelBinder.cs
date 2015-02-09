// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core.Collections;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Modelbinder to bind form values to <see cref="IFormCollection"/>.
    /// </summary>
    public class FormCollectionModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<bool> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(IFormCollection) &&
                bindingContext.ModelType != typeof(FormCollection))
            {
                return false;
            }

            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                if (bindingContext.ModelType.IsAssignableFrom(form.GetType()))
                {
                    bindingContext.Model = form;
                }
                else
                {
                    var formValuesLookup = form.ToDictionary(p => p.Key,
                                                             p => p.Value);
                    bindingContext.Model = new FormCollection(formValuesLookup, form.Files);
                }
            }
            else
            {
                bindingContext.Model = new FormCollection(new Dictionary<string, string[]>());
            }

            return true;
        }
    }
}