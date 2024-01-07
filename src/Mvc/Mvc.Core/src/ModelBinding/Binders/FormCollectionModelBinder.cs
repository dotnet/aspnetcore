// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// <see cref="IModelBinder"/> implementation to bind form values to <see cref="IFormCollection"/>.
/// </summary>
public class FormCollectionModelBinder : IModelBinder
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FormCollectionModelBinder"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public FormCollectionModelBinder(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger(typeof(FormCollectionModelBinder));
    }

    /// <inheritdoc />
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

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

    private sealed class EmptyFormCollection : IFormCollection
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

    private sealed class EmptyFormFileCollection : List<IFormFile>, IFormFileCollection
    {
        public IFormFile? this[string name] => null;

        public IFormFile? GetFile(string name) => null;

        IReadOnlyList<IFormFile> IFormFileCollection.GetFiles(string name) => Array.Empty<IFormFile>();
    }
}
