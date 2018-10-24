// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind form values to <see cref="IFormCollection"/>.
    /// </summary>
    public class FormCollectionModelBinder : IModelBinder
    {
        private readonly ILogger _logger;

        /// <summary>
        /// <para>This constructor is obsolete and will be removed in a future version. The recommended alternative
        /// is the overload that takes an <see cref="ILoggerFactory"/>.</para>
        /// <para>Initializes a new instance of <see cref="FormCollectionModelBinder"/>.</para>
        /// </summary>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended alternative"
            + " is the overload that takes an " + nameof(ILoggerFactory) + ".")]
        public FormCollectionModelBinder()
            : this(NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FormCollectionModelBinder"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public FormCollectionModelBinder(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<FormCollectionModelBinder>();
        }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.AttemptingToBindModel(bindingContext);

            object model;
            var request = bindingContext.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                model = form;
            }
            else
            {
                _logger.CannotBindToFilesCollectionDueToUnsupportedContentType(bindingContext);
                model = new EmptyFormCollection();
            }

            bindingContext.Result = ModelBindingResult.Success(model);
            _logger.DoneAttemptingToBindModel(bindingContext);
        }

        private class EmptyFormCollection : IFormCollection
        {
            public StringValues this[string key] => StringValues.Empty;

            public int Count => 0;

            public IFormFileCollection Files => new EmptyFormFileCollection();

            public ICollection<string> Keys => new List<string>();

            public bool ContainsKey(string key)
            {
                return false;
            }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            {
                return Enumerable.Empty<KeyValuePair<string, StringValues>>().GetEnumerator();
            }

            public bool TryGetValue(string key, out StringValues value)
            {
                value = default(StringValues);
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class EmptyFormFileCollection : List<IFormFile>, IFormFileCollection
        {
            public IFormFile this[string name] => null;

            public IFormFile GetFile(string name) => null;

            IReadOnlyList<IFormFile> IFormFileCollection.GetFiles(string name) => null;
        }
    }
}