// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// <see cref="IModelBinder"/> implementation for binding dictionary values.
/// </summary>
/// <typeparam name="TKey">Type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">Type of values in the dictionary.</typeparam>
public partial class DictionaryModelBinder<TKey, TValue> : CollectionModelBinder<KeyValuePair<TKey, TValue?>> where TKey : notnull
{
    private readonly IModelBinder _valueBinder;

    /// <summary>
    /// Creates a new <see cref="DictionaryModelBinder{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
    /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DictionaryModelBinder(IModelBinder keyBinder, IModelBinder valueBinder, ILoggerFactory loggerFactory)
        : base(new KeyValuePairModelBinder<TKey, TValue>(keyBinder, valueBinder, loggerFactory), loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(valueBinder);

        _valueBinder = valueBinder;
    }

    /// <summary>
    /// Creates a new <see cref="DictionaryModelBinder{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
    /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="allowValidatingTopLevelNodes">
    /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
    /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
    /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
    /// </param>
    /// <remarks>
    /// The <paramref name="allowValidatingTopLevelNodes"/> parameter is currently ignored.
    /// <see cref="CollectionModelBinder{TElement}.AllowValidatingTopLevelNodes"/> is always
    /// <see langword="false"/> in <see cref="DictionaryModelBinder{TKey, TValue}"/>. This class ignores that
    /// property and unconditionally checks for unbound top-level models with
    /// <see cref="ModelMetadata.IsBindingRequired"/>.
    /// </remarks>
    public DictionaryModelBinder(
        IModelBinder keyBinder,
        IModelBinder valueBinder,
        ILoggerFactory loggerFactory,
        bool allowValidatingTopLevelNodes)
        : base(
            new KeyValuePairModelBinder<TKey, TValue>(keyBinder, valueBinder, loggerFactory),
            loggerFactory,
            // CollectionModelBinder should not check IsRequired, done in this model binder.
            allowValidatingTopLevelNodes: false)
    {
        ArgumentNullException.ThrowIfNull(valueBinder);

        _valueBinder = valueBinder;
    }

    /// <summary>
    /// Creates a new <see cref="DictionaryModelBinder{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyBinder">The <see cref="IModelBinder"/> for <typeparamref name="TKey"/>.</param>
    /// <param name="valueBinder">The <see cref="IModelBinder"/> for <typeparamref name="TValue"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="allowValidatingTopLevelNodes">
    /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
    /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
    /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
    /// </param>
    /// <param name="mvcOptions">The <see cref="MvcOptions"/>.</param>
    /// <remarks>
    /// <para>This is the preferred <see cref="DictionaryModelBinder{TKey, TValue}"/> constructor.</para>
    /// <para>
    /// The <paramref name="allowValidatingTopLevelNodes"/> parameter is currently ignored.
    /// <see cref="CollectionModelBinder{TElement}.AllowValidatingTopLevelNodes"/> is always
    /// <see langword="false"/> in <see cref="DictionaryModelBinder{TKey, TValue}"/>. This class ignores that
    /// property and unconditionally checks for unbound top-level models with
    /// <see cref="ModelMetadata.IsBindingRequired"/>.
    /// </para>
    /// </remarks>
    public DictionaryModelBinder(
        IModelBinder keyBinder,
        IModelBinder valueBinder,
        ILoggerFactory loggerFactory,
        bool allowValidatingTopLevelNodes,
        MvcOptions mvcOptions)
        : base(
              new KeyValuePairModelBinder<TKey, TValue>(keyBinder, valueBinder, loggerFactory),
              loggerFactory,
              allowValidatingTopLevelNodes: false,
              mvcOptions)
    {
        ArgumentNullException.ThrowIfNull(valueBinder);

        _valueBinder = valueBinder;
    }

    /// <inheritdoc />
    public override async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        await base.BindModelAsync(bindingContext);
        var result = bindingContext.Result;

        if (result.IsModelSet)
        {
            Debug.Assert(result.Model != null);
            if (result.Model is IDictionary<TKey, TValue?> { Count: > 0 })
            {
                // ICollection<KeyValuePair<TKey, TValue>> approach was successful.
                return;
            }
        }

        Log.NoKeyValueFormatForDictionaryModelBinder(Logger, bindingContext);

        if (bindingContext.ValueProvider is not IEnumerableValueProvider enumerableValueProvider)
        {
            // No IEnumerableValueProvider available for the fallback approach. For example the user may have
            // replaced the ValueProvider with something other than a CompositeValueProvider.
            if (bindingContext.IsTopLevelObject)
            {
                AddErrorIfBindingRequired(bindingContext);
            }

            // No match for the prefix at all.
            return;
        }

        // Attempt to bind dictionary from a set of prefix[key]=value entries. Get the short and long keys first.
        var prefix = bindingContext.ModelName;
        var keys = enumerableValueProvider.GetKeysFromPrefix(prefix);
        if (keys.Count == 0)
        {
            // No entries with the expected keys.
            if (bindingContext.IsTopLevelObject)
            {
                AddErrorIfBindingRequired(bindingContext);
            }

            return;
        }

        // Update the existing successful but empty ModelBindingResult.
        var model = (IDictionary<TKey, TValue?>)(result.Model ?? CreateEmptyCollection(bindingContext.ModelType));
        var elementMetadata = bindingContext.ModelMetadata.ElementMetadata!;
        var valueMetadata = elementMetadata.Properties[nameof(KeyValuePair<TKey, TValue>.Value)]!;

        var keyMappings = new Dictionary<string, TKey>(StringComparer.Ordinal);
        foreach (var kvp in keys)
        {
            // Use InvariantCulture to convert the key since ExpressionHelper.GetExpressionText() would use
            // that culture when rendering a form.
            TKey? convertedKey;
            try
            {
                convertedKey = ModelBindingHelper.ConvertTo<TKey>(kvp.Key, culture: null);
            }
            catch (Exception ex)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex.Message);
                return;
            }

            using (bindingContext.EnterNestedScope(
                modelMetadata: valueMetadata,
                fieldName: bindingContext.FieldName,
                modelName: kvp.Value,
                model: null))
            {
                await _valueBinder.BindModelAsync(bindingContext);

                var valueResult = bindingContext.Result;
                if (!valueResult.IsModelSet)
                {
                    // Factories for IKeyRewriterValueProvider implementations are not all-or-nothing i.e.
                    // "[key][propertyName]" may be rewritten as ".key.propertyName" or "[key].propertyName". Try
                    // again in case this scope is binding a complex type and rewriting
                    // landed on ".key.propertyName" or in case this scope is binding another collection and an
                    // IKeyRewriterValueProvider implementation was first (hiding the original "[key][next key]").
                    if (kvp.Value.EndsWith(']'))
                    {
                        bindingContext.ModelName = ModelNames.CreatePropertyModelName(prefix, kvp.Key);
                    }
                    else
                    {
                        bindingContext.ModelName = ModelNames.CreateIndexModelName(prefix, kvp.Key);
                    }

                    await _valueBinder.BindModelAsync(bindingContext);
                    valueResult = bindingContext.Result;
                }

                // Always add an entry to the dictionary but validate only if binding was successful.
                model[convertedKey] = ModelBindingHelper.CastOrDefault<TValue>(valueResult.Model);
                keyMappings.Add(bindingContext.ModelName, convertedKey);
            }
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        bindingContext.ValidationState.Add(model, new ValidationStateEntry()
        {
            Strategy = new ShortFormDictionaryValidationStrategy<TKey, TValue?>(keyMappings, valueMetadata),
        });
    }

    /// <inheritdoc />
    protected override object? ConvertToCollectionType(
        Type targetType,
        IEnumerable<KeyValuePair<TKey, TValue?>> collection)
    {
        if (collection == null)
        {
            return null;
        }

        if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue?>)))
        {
            // Collection is a List<KeyValuePair<TKey, TValue>>, never already a Dictionary<TKey, TValue>.
            return collection.ToDictionary();
        }

        return base.ConvertToCollectionType(targetType, collection);
    }

    /// <inheritdoc />
    protected override object CreateEmptyCollection(Type targetType)
    {
        if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
        {
            // Simple case such as IDictionary<TKey, TValue>.
            return new Dictionary<TKey, TValue>();
        }

        return base.CreateEmptyCollection(targetType);
    }

    /// <inheritdoc/>
    public override bool CanCreateInstance(Type targetType)
    {
        if (targetType.IsAssignableFrom(typeof(Dictionary<TKey, TValue>)))
        {
            // Simple case such as IDictionary<TKey, TValue>.
            return true;
        }

        return base.CanCreateInstance(targetType);
    }

    private static partial class Log
    {
        public static void NoKeyValueFormatForDictionaryModelBinder(ILogger logger, ModelBindingContext bindingContext)
            => NoKeyValueFormatForDictionaryModelBinder(logger, bindingContext.ModelName);

        [LoggerMessage(33, LogLevel.Debug, "Attempting to bind model with name '{ModelName}' using the format {ModelName}[key1]=value1&{ModelName}[key2]=value2", EventName = "NoKeyValueFormatForDictionaryModelBinder")]
        private static partial void NoKeyValueFormatForDictionaryModelBinder(ILogger logger, string modelName);
    }
}
