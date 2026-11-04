// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

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
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger(typeof(ByteArrayModelBinder));
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

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
