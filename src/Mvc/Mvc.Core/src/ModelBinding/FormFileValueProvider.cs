// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> adapter for data stored in an <see cref="IFormFileCollection"/>.
    /// </summary>
    /// <remarks>
    /// Unlike most <see cref="IValueProvider"/> instances, <see cref="FormFileValueProvider"/> does not provide any values, but
    /// specifically responds to <see cref="ContainsPrefix(string)"/> queries. This allows the model binding system to
    /// recurse in to deeply nested object graphs with only values for form files.
    /// </remarks>
    public sealed class FormFileValueProvider : IValueProvider
    {
        private readonly IFormFileCollection _files;
        private PrefixContainer _prefixContainer;

        /// <summary>
        /// Creates a value provider for <see cref="IFormFileCollection"/>.
        /// </summary>
        /// <param name="files">The <see cref="IFormFileCollection"/>.</param>
        public FormFileValueProvider(IFormFileCollection files)
        {
            _files = files ?? throw new ArgumentNullException(nameof(files));
        }

        private PrefixContainer PrefixContainer
        {
            get
            {
                _prefixContainer ??= CreatePrefixContainer(_files);
                return _prefixContainer;
            }
        }

        private static PrefixContainer CreatePrefixContainer(IFormFileCollection formFiles)
        {
            var fileNames = new List<string>();
            var count = formFiles.Count;
            for (var i = 0; i < count; i++)
            {
                var file = formFiles[i];

                // If there is an <input type="file" ... /> in the form and is left blank.
                // This matches the filtering behavior from FormFileModelBinder
                if (file.Length == 0 && string.IsNullOrEmpty(file.FileName))
                {
                    continue;
                }

                fileNames.Add(file.Name);
            }

            return new PrefixContainer(fileNames);
        }

        /// <inheritdoc />
        public bool ContainsPrefix(string prefix) => PrefixContainer.ContainsPrefix(prefix);

        /// <inheritdoc />
        public ValueProviderResult GetValue(string key) => ValueProviderResult.None;
    }
}
