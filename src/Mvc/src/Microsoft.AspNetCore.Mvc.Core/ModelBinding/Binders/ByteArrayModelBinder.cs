// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// ModelBinder to bind byte Arrays.
    /// </summary>
    public class ByteArrayModelBinder : IModelBinder
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ByteArrayModelBinder"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public ByteArrayModelBinder(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<ByteArrayModelBinder>();
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.AttemptingToBindModel(bindingContext);

            // Check for missing data case 1: There was no <input ... /> element containing this data.
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                _logger.FoundNoValueInRequest(bindingContext);
                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            // Check for missing data case 2: There was an <input ... /> element but it was left blank.
            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                _logger.FoundNoValueInRequest(bindingContext);
                _logger.DoneAttemptingToBindModel(bindingContext);
                return Task.CompletedTask;
            }

            try
            {
                var model = Convert.FromBase64String(value);
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            catch (Exception exception)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    exception,
                    bindingContext.ModelMetadata);
            }

            _logger.DoneAttemptingToBindModel(bindingContext);
            return Task.CompletedTask;
        }
    }
}