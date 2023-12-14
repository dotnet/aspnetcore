// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

internal static class ModelBindingHelper
{
    /// <summary>
    /// Updates the specified <paramref name="model"/> instance using the specified
    /// <paramref name="modelBinderFactory"/> and the specified <paramref name="valueProvider"/> and executes
    /// validation using the specified <paramref name="objectModelValidator"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object.</typeparam>
    /// <param name="model">The model instance to update and validate.</param>
    /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
    /// </param>
    /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
    /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
    /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
    /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
    /// bound values.</param>
    /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
    public static Task<bool> TryUpdateModelAsync<TModel>(
        TModel model,
        string prefix,
        ActionContext actionContext,
        IModelMetadataProvider metadataProvider,
        IModelBinderFactory modelBinderFactory,
        IValueProvider valueProvider,
        IObjectModelValidator objectModelValidator)
        where TModel : class
    {
        return TryUpdateModelAsync(
            model,
            prefix,
            actionContext,
            metadataProvider,
            modelBinderFactory,
            valueProvider,
            objectModelValidator,
            // Includes everything by default.
            propertyFilter: (m) => true);
    }

    /// <summary>
    /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
    /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
    /// <paramref name="objectModelValidator"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object.</typeparam>
    /// <param name="model">The model instance to update and validate.</param>
    /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
    /// </param>
    /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
    /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
    /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
    /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
    /// bound values.</param>
    /// <param name="includeExpressions">Expression(s) which represent top level properties
    /// which need to be included for the current model.</param>
    /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
    public static Task<bool> TryUpdateModelAsync<TModel>(
        TModel model,
        string prefix,
        ActionContext actionContext,
        IModelMetadataProvider metadataProvider,
        IModelBinderFactory modelBinderFactory,
        IValueProvider valueProvider,
        IObjectModelValidator objectModelValidator,
        params Expression<Func<TModel, object?>>[] includeExpressions)
       where TModel : class
    {
        ArgumentNullException.ThrowIfNull(includeExpressions);

        var expression = GetPropertyFilterExpression(includeExpressions);
        var propertyFilter = expression.Compile();

        return TryUpdateModelAsync(
           model,
           prefix,
           actionContext,
           metadataProvider,
           modelBinderFactory,
           valueProvider,
           objectModelValidator,
           propertyFilter);
    }

    /// <summary>
    /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
    /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
    /// <paramref name="objectModelValidator"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model object.</typeparam>
    /// <param name="model">The model instance to update and validate.</param>
    /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
    /// </param>
    /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
    /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
    /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
    /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
    /// bound values.</param>
    /// <param name="propertyFilter">
    /// A predicate which can be used to filter properties(for inclusion/exclusion) at runtime.
    /// </param>
    /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
    public static Task<bool> TryUpdateModelAsync<TModel>(
        TModel model,
        string prefix,
        ActionContext actionContext,
        IModelMetadataProvider metadataProvider,
        IModelBinderFactory modelBinderFactory,
        IValueProvider valueProvider,
        IObjectModelValidator objectModelValidator,
        Func<ModelMetadata, bool> propertyFilter)
        where TModel : class
    {
        return TryUpdateModelAsync(
           model,
           typeof(TModel),
           prefix,
           actionContext,
           metadataProvider,
           modelBinderFactory,
           valueProvider,
           objectModelValidator,
           propertyFilter);
    }

    /// <summary>
    /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
    /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
    /// <paramref name="objectModelValidator"/>.
    /// </summary>
    /// <param name="model">The model instance to update and validate.</param>
    /// <param name="modelType">The type of model instance to update and validate.</param>
    /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
    /// </param>
    /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
    /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
    /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
    /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
    /// bound values.</param>
    /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
    public static Task<bool> TryUpdateModelAsync(
        object model,
        Type modelType,
        string prefix,
        ActionContext actionContext,
        IModelMetadataProvider metadataProvider,
        IModelBinderFactory modelBinderFactory,
        IValueProvider valueProvider,
        IObjectModelValidator objectModelValidator)
    {
        return TryUpdateModelAsync(
            model,
            modelType,
            prefix,
            actionContext,
            metadataProvider,
            modelBinderFactory,
            valueProvider,
            objectModelValidator,
            // Includes everything by default.
            propertyFilter: (m) => true);
    }

    /// <summary>
    /// Updates the specified <paramref name="model"/> instance using the specified <paramref name="modelBinderFactory"/>
    /// and the specified <paramref name="valueProvider"/> and executes validation using the specified
    /// <paramref name="objectModelValidator"/>.
    /// </summary>
    /// <param name="model">The model instance to update and validate.</param>
    /// <param name="modelType">The type of model instance to update and validate.</param>
    /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
    /// </param>
    /// <param name="actionContext">The <see cref="ActionContext"/> for the current executing request.</param>
    /// <param name="metadataProvider">The provider used for reading metadata for the model type.</param>
    /// <param name="modelBinderFactory">The <see cref="IModelBinderFactory"/> used for binding.</param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
    /// <param name="objectModelValidator">The <see cref="IObjectModelValidator"/> used for validating the
    /// bound values.</param>
    /// <param name="propertyFilter">A predicate which can be used to
    /// filter properties(for inclusion/exclusion) at runtime.</param>
    /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful</returns>
    public static async Task<bool> TryUpdateModelAsync(
        object model,
        Type modelType,
        string prefix,
        ActionContext actionContext,
        IModelMetadataProvider metadataProvider,
        IModelBinderFactory modelBinderFactory,
        IValueProvider valueProvider,
        IObjectModelValidator objectModelValidator,
        Func<ModelMetadata, bool> propertyFilter)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(metadataProvider);
        ArgumentNullException.ThrowIfNull(modelBinderFactory);
        ArgumentNullException.ThrowIfNull(valueProvider);
        ArgumentNullException.ThrowIfNull(objectModelValidator);
        ArgumentNullException.ThrowIfNull(propertyFilter);

        if (!modelType.IsAssignableFrom(model.GetType()))
        {
            var message = Resources.FormatModelType_WrongType(
                model.GetType().FullName,
                modelType.FullName);
            throw new ArgumentException(message, nameof(modelType));
        }

        var modelMetadata = metadataProvider.GetMetadataForType(modelType);

        if (modelMetadata.BoundConstructor != null)
        {
            throw new NotSupportedException(Resources.FormatTryUpdateModel_RecordTypeNotSupported(nameof(TryUpdateModelAsync), modelType));
        }

        var modelState = actionContext.ModelState;

        var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            valueProvider,
            modelMetadata,
            bindingInfo: null,
            modelName: prefix);

        modelBindingContext.Model = model;
        modelBindingContext.PropertyFilter = propertyFilter;

        var factoryContext = new ModelBinderFactoryContext()
        {
            Metadata = modelMetadata,
            BindingInfo = new BindingInfo()
            {
                BinderModelName = modelMetadata.BinderModelName,
                BinderType = modelMetadata.BinderType,
                BindingSource = modelMetadata.BindingSource,
                PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
            },

            // We're using the model metadata as the cache token here so that TryUpdateModelAsync calls
            // for the same model type can share a binder. This won't overlap with normal model binding
            // operations because they use the ParameterDescriptor for the token.
            CacheToken = modelMetadata,
        };
        var binder = modelBinderFactory.CreateBinder(factoryContext);

        await binder.BindModelAsync(modelBindingContext);
        var modelBindingResult = modelBindingContext.Result;
        if (modelBindingResult.IsModelSet)
        {
            objectModelValidator.Validate(
                actionContext,
                modelBindingContext.ValidationState,
                modelBindingContext.ModelName,
                modelBindingResult.Model);

            return modelState.IsValid;
        }

        return false;
    }

    // Internal for tests
    internal static string GetPropertyName(Expression expression)
    {
        if (expression.NodeType == ExpressionType.Convert ||
            expression.NodeType == ExpressionType.ConvertChecked)
        {
            // For Boxed Value Types
            expression = ((UnaryExpression)expression).Operand;
        }

        if (expression.NodeType != ExpressionType.MemberAccess)
        {
            throw new InvalidOperationException(
                Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
        }

        var memberExpression = (MemberExpression)expression;
        if (memberExpression.Member is PropertyInfo memberInfo)
        {
            if (memberExpression.Expression!.NodeType != ExpressionType.Parameter)
            {
                // Chained expressions and non parameter based expressions are not supported.
                throw new InvalidOperationException(
                    Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
            }

            return memberInfo.Name;
        }
        else
        {
            // Fields are also not supported.
            throw new InvalidOperationException(
                Resources.FormatInvalid_IncludePropertyExpression(expression.NodeType));
        }
    }

    /// <summary>
    /// Creates an expression for a predicate to limit the set of properties used in model binding.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="expressions">Expressions identifying the properties to allow for binding.</param>
    /// <returns>An expression which can be used with <see cref="IPropertyFilterProvider"/>.</returns>
    public static Expression<Func<ModelMetadata, bool>> GetPropertyFilterExpression<TModel>(
        Expression<Func<TModel, object?>>[] expressions)
    {
        if (expressions.Length == 0)
        {
            // If nothing is included explicitly, treat everything as included.
            return (m) => true;
        }

        var firstExpression = GetPredicateExpression(expressions[0]);
        var orWrapperExpression = firstExpression.Body;
        foreach (var expression in expressions.Skip(1))
        {
            var predicate = GetPredicateExpression(expression);
            orWrapperExpression = Expression.OrElse(
                orWrapperExpression,
                Expression.Invoke(predicate, firstExpression.Parameters));
        }

        return Expression.Lambda<Func<ModelMetadata, bool>>(orWrapperExpression, firstExpression.Parameters);
    }

    private static Expression<Func<ModelMetadata, bool>> GetPredicateExpression<TModel>(
        Expression<Func<TModel, object?>> expression)
    {
        var propertyName = GetPropertyName(expression.Body);

        return (metadata) => string.Equals(metadata.PropertyName, propertyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Clears <see cref="ModelStateDictionary"/> entries for <see cref="ModelMetadata"/>.
    /// </summary>
    /// <param name="modelType">The <see cref="Type"/> of the model.</param>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> associated with the model.</param>
    /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    /// <param name="modelKey">The entry to clear. </param>
    public static void ClearValidationStateForModel(
        Type modelType,
        ModelStateDictionary modelState,
        IModelMetadataProvider metadataProvider,
        string modelKey)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(metadataProvider);

        ClearValidationStateForModel(metadataProvider.GetMetadataForType(modelType), modelState, modelKey);
    }

    /// <summary>
    /// Clears <see cref="ModelStateDictionary"/> entries for <see cref="ModelMetadata"/>.
    /// </summary>
    /// <param name="modelMetadata">The <see cref="ModelMetadata"/>.</param>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> associated with the model.</param>
    /// <param name="modelKey">The entry to clear. </param>
    public static void ClearValidationStateForModel(
        ModelMetadata modelMetadata,
        ModelStateDictionary modelState,
        string? modelKey)
    {
        ArgumentNullException.ThrowIfNull(modelMetadata);
        ArgumentNullException.ThrowIfNull(modelState);

        if (string.IsNullOrEmpty(modelKey))
        {
            // If model key is empty, we have to do a best guess to try and clear the appropriate
            // keys. Clearing the empty prefix would clear the state of ALL entries, which might wipe out
            // data from other models.
            if (modelMetadata.IsEnumerableType)
            {
                // We expect that any key beginning with '[' is an index. We can't just infer the indexes
                // used, so we clear all keys that look like <empty prefix -> index>.
                //
                // In the unlikely case that multiple top-level collections where bound to the empty prefix,
                // you're just out of luck.
                foreach (var kvp in modelState)
                {
                    if (kvp.Key.Length > 0 && kvp.Key[0] == '[')
                    {
                        // Starts with an indexer
                        kvp.Value.Errors.Clear();
                        kvp.Value.ValidationState = ModelValidationState.Unvalidated;
                    }
                }
            }
            else if (modelMetadata.IsComplexType)
            {
                for (var i = 0; i < modelMetadata.Properties.Count; i++)
                {
                    var property = modelMetadata.Properties[i];
                    modelState.ClearValidationState((property.BinderModelName ?? property.PropertyName)!);
                }
            }
            else
            {
                // Simple types bind to a single entry. So clear the entry with the empty-key, in the
                // unlikely event that it has errors.
                var entry = modelState[string.Empty];
                if (entry != null)
                {
                    entry.Errors.Clear();
                    entry.ValidationState = ModelValidationState.Unvalidated;
                }
            }
        }
        else
        {
            // If model key is non-empty, we just want to clear all keys with that prefix. We expect
            // model binding to have only used this key (and suffixes) for all entries related to
            // this model.
            modelState.ClearValidationState(modelKey);
        }
    }

    internal static TModel? CastOrDefault<TModel>(object? model)
    {
        return (model is TModel tModel) ? tModel : default;
    }

    /// <summary>
    /// Gets an indication whether <see cref="M:GetCompatibleCollection{T}"/> is likely to return a usable
    /// non-<c>null</c> value.
    /// </summary>
    /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <returns>
    /// <c>true</c> if <see cref="M:GetCompatibleCollection{T}"/> is likely to return a usable non-<c>null</c>
    /// value; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>"Usable" in this context means the property can be set or its value reused.</remarks>
    public static bool CanGetCompatibleCollection<T>(ModelBindingContext bindingContext)
    {
        var model = bindingContext.Model;
        var modelType = bindingContext.ModelType;

        if (typeof(T).IsAssignableFrom(modelType))
        {
            // Scalar case. Existing model is not relevant and property must always be set. Will use a List<T>
            // intermediate and set property to first element, if any.
            return true;
        }

        if (modelType == typeof(T[]))
        {
            // Can't change the length of an existing array or replace it. Will use a List<T> intermediate and set
            // property to an array created from that.
            return true;
        }

        if (!typeof(IEnumerable<T>).IsAssignableFrom(modelType))
        {
            // Not a supported collection.
            return false;
        }

        if (model is ICollection<T> collection && !collection.IsReadOnly)
        {
            // Can use the existing collection.
            return true;
        }

        // Most likely the model is null.
        // Also covers the corner case where the model implements IEnumerable<T> but not ICollection<T> e.g.
        //   public IEnumerable<T> Property { get; set; } = new T[0];
        if (modelType.IsAssignableFrom(typeof(List<T>)))
        {
            return true;
        }

        // Will we be able to activate an instance and use that?
        return modelType.IsClass &&
            !modelType.IsAbstract &&
            typeof(ICollection<T>).IsAssignableFrom(modelType);
    }

    /// <summary>
    /// Creates an <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
    /// <see cref="ModelBindingContext.ModelType"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <returns>
    /// An <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
    /// <see cref="ModelBindingContext.ModelType"/>.
    /// </returns>
    /// <remarks>
    /// Should not be called if <see cref="CanGetCompatibleCollection{T}"/> returned <c>false</c>.
    /// </remarks>
    public static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext)
    {
        return GetCompatibleCollection<T>(bindingContext, capacity: null);
    }

    /// <summary>
    /// Creates an <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
    /// <see cref="ModelBindingContext.ModelType"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the <see cref="ICollection{T}"/> required.</typeparam>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <param name="capacity">
    /// Capacity for use when creating a <see cref="List{T}"/> instance. Not used when creating another type.
    /// </param>
    /// <returns>
    /// An <see cref="ICollection{T}"/> instance compatible with <paramref name="bindingContext"/>'s
    /// <see cref="ModelBindingContext.ModelType"/>.
    /// </returns>
    /// <remarks>
    /// Should not be called if <see cref="CanGetCompatibleCollection{T}"/> returned <c>false</c>.
    /// </remarks>
    public static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext, int capacity)
    {
        return GetCompatibleCollection<T>(bindingContext, (int?)capacity);
    }

    private static ICollection<T> GetCompatibleCollection<T>(ModelBindingContext bindingContext, int? capacity)
    {
        var model = bindingContext.Model;
        var modelType = bindingContext.ModelType;

        // There's a limited set of collection types we can create here.
        //
        // For the simple cases: Choose List<T> if the destination type supports it (at least as an intermediary).
        //
        // For more complex cases: If the destination type is a class that implements ICollection<T>, then activate
        // an instance and return that.
        //
        // Otherwise just give up.
        if (typeof(T).IsAssignableFrom(modelType))
        {
            return CreateList<T>(capacity);
        }

        if (modelType == typeof(T[]))
        {
            return CreateList<T>(capacity);
        }

        // Does collection exist and can it be reused?
        if (model is ICollection<T> collection && !collection.IsReadOnly)
        {
            collection.Clear();

            return collection;
        }

        if (modelType.IsAssignableFrom(typeof(List<T>)))
        {
            return CreateList<T>(capacity);
        }

        return (ICollection<T>)Activator.CreateInstance(modelType)!;
    }

    private static List<T> CreateList<T>(int? capacity)
    {
        return capacity.HasValue ? new List<T>(capacity.Value) : new List<T>();
    }

    /// <summary>
    /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> for conversion.</typeparam>
    /// <param name="value">The value to convert."/></param>
    /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
    /// <returns>
    /// The converted value or the default value of <typeparamref name="T"/> if the value could not be converted.
    /// </returns>
    [return: NotNullIfNotNull("value")]
    public static T? ConvertTo<T>(object? value, CultureInfo? culture)
    {
        var converted = ConvertTo(value, typeof(T), culture);
        return converted == null ? default : (T)converted;
    }

    /// <summary>
    /// Converts the provided <paramref name="value"/> to a value of <see cref="Type"/> <paramref name="type"/>.
    /// </summary>
    /// <param name="value">The value to convert."/></param>
    /// <param name="type">The <see cref="Type"/> for conversion.</param>
    /// <param name="culture">The <see cref="CultureInfo"/> for conversion.</param>
    /// <returns>
    /// The converted value or <c>null</c> if the value could not be converted.
    /// </returns>
    public static object? ConvertTo(object? value, Type type, CultureInfo? culture)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (value == null)
        {
            // For value types, treat null values as though they were the default value for the type.
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        if (type.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        var cultureToUse = culture ?? CultureInfo.InvariantCulture;
        return UnwrapPossibleArrayType(value, type, cultureToUse);
    }

    private static object? UnwrapPossibleArrayType(object value, Type destinationType, CultureInfo culture)
    {
        // array conversion results in four cases, as below
        var valueAsArray = value as Array;
        if (destinationType.IsArray)
        {
            var destinationElementType = destinationType.GetElementType()!;
            if (valueAsArray != null)
            {
                // case 1: both destination + source type are arrays, so convert each element
                var converted = (IList)Array.CreateInstance(destinationElementType, valueAsArray.Length);
                for (var i = 0; i < valueAsArray.Length; i++)
                {
                    converted[i] = ConvertSimpleType(valueAsArray.GetValue(i), destinationElementType, culture);
                }
                return converted;
            }
            else
            {
                // case 2: destination type is array but source is single element, so wrap element in
                // array + convert
                var element = ConvertSimpleType(value, destinationElementType, culture);
                var converted = (IList)Array.CreateInstance(destinationElementType, 1);
                converted[0] = element;
                return converted;
            }
        }
        else if (valueAsArray != null)
        {
            // case 3: destination type is single element but source is array, so extract first element + convert
            if (valueAsArray.Length > 0)
            {
                var elementValue = valueAsArray.GetValue(0);
                return ConvertSimpleType(elementValue, destinationType, culture);
            }
            else
            {
                // case 3(a): source is empty array, so can't perform conversion
                return null;
            }
        }

        // case 4: both destination + source type are single elements, so convert
        return ConvertSimpleType(value, destinationType, culture);
    }

    private static object? ConvertSimpleType(object? value, Type destinationType, CultureInfo culture)
    {
        if (value == null || destinationType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        // In case of a Nullable object, we try again with its underlying type.
        destinationType = UnwrapNullableType(destinationType);

        // if this is a user-input value but the user didn't type anything, return no value
        if (value is string valueAsString && string.IsNullOrWhiteSpace(valueAsString))
        {
            return null;
        }

        var converter = TypeDescriptor.GetConverter(destinationType);
        var canConvertFrom = converter.CanConvertFrom(value.GetType());
        if (!canConvertFrom)
        {
            converter = TypeDescriptor.GetConverter(value.GetType());
        }
        if (!(canConvertFrom || converter.CanConvertTo(destinationType)))
        {
            // EnumConverter cannot convert integer, so we verify manually
            if (destinationType.IsEnum &&
                (value is int ||
                value is uint ||
                value is long ||
                value is ulong ||
                value is short ||
                value is ushort ||
                value is byte ||
                value is sbyte))
            {
                return Enum.ToObject(destinationType, value);
            }

            throw new InvalidOperationException(
                Resources.FormatValueProviderResult_NoConverterExists(value.GetType(), destinationType));
        }

        try
        {
            return canConvertFrom
                ? converter.ConvertFrom(null, culture, value)
                : converter.ConvertTo(null, culture, value, destinationType);
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (ex.InnerException == null)
            {
                throw;
            }
            else
            {
                // TypeConverter throws System.Exception wrapping the FormatException,
                // so we throw the inner exception.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();

                // This code is never reached because the previous line will always throw.
                throw;
            }
        }
    }

    private static Type UnwrapNullableType(Type destinationType)
    {
        return Nullable.GetUnderlyingType(destinationType) ?? destinationType;
    }
}
