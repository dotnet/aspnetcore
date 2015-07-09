// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind form values to <see cref="IFormCollection"/>.
    /// </summary>
    public class FormCollectionModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(IFormCollection))
            {
                return null;
            }

            object model;
            var request = bindingContext.OperationBindingContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                model = form;
            }
            else
            {
                model = new EmptyFormCollection();
            }

            var validationNode =
                 new ModelValidationNode(bindingContext.ModelName, bindingContext.ModelMetadata, model)
                 {
                     SuppressValidation = true,
                 };

            return new ModelBindingResult(
                model,
                bindingContext.ModelName,
                isModelSet: true,
                validationNode: validationNode);
        }

        private class EmptyFormCollection : IFormCollection
        {
            public string this[string key]
            {
                get
                {
                    return null;
                }
            }

            public int Count
            {
                get
                {
                    return 0;
                }
            }

            public IFormFileCollection Files
            {
                get
                {
                    return new EmptyFormFileCollection();
                }
            }

            public ICollection<string> Keys
            {
                get
                {
                    return new List<string>();
                }
            }

            public bool ContainsKey(string key)
            {
                return false;
            }

            public string Get(string key)
            {
                return null;
            }

            public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
            {
                return Enumerable.Empty<KeyValuePair<string, string[]>>().GetEnumerator();
            }

            public IList<string> GetValues(string key)
            {
                return null;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class EmptyFormFileCollection : List<IFormFile>, IFormFileCollection
        {
            public IFormFile this[string name]
            {
                get
                {
                    return null;
                }
            }

            public IFormFile GetFile(string name)
            {
                return null;
            }

            IReadOnlyList<IFormFile> IFormFileCollection.GetFiles(string name)
            {
                throw new NotImplementedException();
            }
        }
    }
}