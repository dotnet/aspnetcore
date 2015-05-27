// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Modelbinder to bind posted files to <see cref="IFormFile"/>.
    /// </summary>
    public class FormFileModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(IFormFile))
            {
                var postedFiles = await GetFormFilesAsync(bindingContext);
                var value = postedFiles.FirstOrDefault();
                var validationNode =
                    new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, value);
                return new ModelBindingResult(
                    value,
                    bindingContext.ModelName,
                    isModelSet: value != null,
                    validationNode: validationNode);
            }
            else if (typeof(IEnumerable<IFormFile>).GetTypeInfo().IsAssignableFrom(
                    bindingContext.ModelType.GetTypeInfo()))
            {
                var postedFiles = await GetFormFilesAsync(bindingContext);
                var value = ModelBindingHelper.ConvertValuesToCollectionType(bindingContext.ModelType, postedFiles);
                var validationNode =
                    new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, value);
                return new ModelBindingResult(
                    value,
                    bindingContext.ModelName,
                    isModelSet: value != null,
                    validationNode: validationNode);
            }

            return null;
        }

        private async Task<List<IFormFile>> GetFormFilesAsync(ModelBindingContext bindingContext)
        {
            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            var postedFiles = new List<IFormFile>();
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();

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

                    var modelName = HeaderUtilities.RemoveQuotes(parsedContentDisposition.Name);
                    if (modelName.Equals(bindingContext.ModelName, StringComparison.OrdinalIgnoreCase))
                    {
                        postedFiles.Add(file);
                    }
                }
            }

            return postedFiles;
        }
    }
}