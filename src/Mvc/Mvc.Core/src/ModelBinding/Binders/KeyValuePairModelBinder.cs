// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for <see cref="KeyValuePair{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
{
    private readonly IModelBinder _keyBinder;
    private readonly IModelBinder _valueBinder;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
    /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public KeyValuePairModelBinder(IModelBinder keyBinder, IModelBinder valueBinder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(keyBinder);
        ArgumentNullException.ThrowIfNull(valueBinder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _keyBinder = keyBinder;
        _valueBinder = valueBinder;
        _logger = loggerFactory.CreateLogger(typeof(KeyValuePairModelBinder<TKey, TValue>));
    }

    /// <inheritdoc />
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        _logger.AttemptingToBindModel(bindingContext);

        var keyModelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Key");
        var keyResult = await KeyValuePairModelBinder<TKey, TValue>.TryBindStrongModel<TKey?>(bindingContext, _keyBinder, "Key", keyModelName);

        var valueModelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, "Value");
        var valueResult = await KeyValuePairModelBinder<TKey, TValue>.TryBindStrongModel<TValue?>(bindingContext, _valueBinder, "Value", valueModelName);

        if (keyResult.IsModelSet && valueResult.IsModelSet)
        {
            var model = new KeyValuePair<TKey?, TValue?>(
                ModelBindingHelper.CastOrDefault<TKey?>(keyResult.Model),
                ModelBindingHelper.CastOrDefault<TValue?>(valueResult.Model));

            bindingContext.Result = ModelBindingResult.Success(model);
            _logger.DoneAttemptingToBindModel(bindingContext);
            return;
        }

        if (!keyResult.IsModelSet && valueResult.IsModelSet)
        {
            bindingContext.ModelState.TryAddModelError(
                keyModelName,
                bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());
            _logger.DoneAttemptingToBindModel(bindingContext);
            return;
        }

        if (keyResult.IsModelSet && !valueResult.IsModelSet)
        {
            bindingContext.ModelState.TryAddModelError(
                valueModelName,
                bindingContext.ModelMetadata.ModelBindingMessageProvider.MissingKeyOrValueAccessor());
            _logger.DoneAttemptingToBindModel(bindingContext);
            return;
        }

        // If we failed to find data for a top-level model, then generate a
        // default 'empty' model and return it.
        if (bindingContext.IsTopLevelObject)
        {
            var model = new KeyValuePair<TKey?, TValue?>();
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        _logger.DoneAttemptingToBindModel(bindingContext);
    }

    internal static async Task<ModelBindingResult> TryBindStrongModel<TModel>(
        ModelBindingContext bindingContext,
        IModelBinder binder,
        string propertyName,
        string propertyModelName)
    {
        var propertyModelMetadata = bindingContext.ModelMetadata.Properties[propertyName]!;

        using (bindingContext.EnterNestedScope(
            modelMetadata: propertyModelMetadata,
            fieldName: propertyName,
            modelName: propertyModelName,
            model: null))
        {
            await binder.BindModelAsync(bindingContext);

            return bindingContext.Result;
        }
    }
}
