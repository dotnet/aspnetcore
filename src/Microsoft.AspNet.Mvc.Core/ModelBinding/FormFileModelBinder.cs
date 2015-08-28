// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind posted files to <see cref="IFormFile"/>.
    /// </summary>
    public class FormFileModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            object value;
            if (bindingContext.ModelType == typeof(IFormFile))
            {
                var postedFiles = await GetFormFilesAsync(bindingContext);
                value = postedFiles.FirstOrDefault();
            }
            else if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(bindingContext.ModelType))
            {
                var postedFiles = await GetFormFilesAsync(bindingContext);
                value = ModelBindingHelper.ConvertValuesToCollectionType(bindingContext.ModelType, postedFiles);
            }
            else
            {
                // This binder does not support the requested type.
                return ModelBindingResult.NoResult;
            }
            
            if (value == null)
            {
                return ModelBindingResult.Failed(bindingContext.ModelName);
            }
            else
            { 
                var validationNode =
                    new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, value)
                    {
                        SuppressValidation = true,
                    };

                bindingContext.ModelState.SetModelValue(
                    bindingContext.ModelName,
                    rawValue: null,
                    attemptedValue: null);

                return ModelBindingResult.Success(bindingContext.ModelName, value, validationNode);
            }
        }

        private async Task<List<IFormFile>> GetFormFilesAsync(ModelBindingContext bindingContext)
        {
            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            var postedFiles = new List<IFormFile>();
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();

                // If we're at the top level, then use the FieldName (paramter or property name).
                // This handles the fact that there will be nothing in the ValueProviders for this parameter
                // and so we'll do the right thing even though we 'fell-back' to the empty prefix.
                var modelName = bindingContext.IsTopLevelObject
                    ? bindingContext.FieldName
                    : bindingContext.ModelName;

                foreach (var file in form.Files)
                {
                    ContentDispositionHeaderValue parsedContentDisposition;
                    ContentDispositionHeaderValue.TryParse(file.ContentDisposition, out parsedContentDisposition);

                    // If there is an <input type="file" ... /> in the form and is left blank.
                    if (parsedContentDisposition == null ||
                        (file.Length == 0 &&
                         string.IsNullOrEmpty(HeaderUtilities.RemoveQuotes(parsedContentDisposition.FileName))))
                    {
                        continue;
                    }

                    var fileName = HeaderUtilities.RemoveQuotes(parsedContentDisposition.Name);
                    if (fileName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        postedFiles.Add(file);
                    }
                }
            }

            return postedFiles;
        }
    }
}