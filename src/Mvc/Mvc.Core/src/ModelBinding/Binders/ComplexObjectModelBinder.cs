// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// <see cref="IModelBinder"/> implementation for binding complex types.
/// </summary>
public sealed partial class ComplexObjectModelBinder : IModelBinder
{
    // Don't want a new public enum because communication between the private and internal methods of this class
    // should not be exposed. Can't use an internal enum because types of [TheoryData] values must be public.

    // Model contains only properties that are expected to bind from value providers and no value provider has
    // matching data.
    internal const int NoDataAvailable = 0;
    // If model contains properties that are expected to bind from value providers, no value provider has matching
    // data. Remaining (greedy) properties might bind successfully.
    internal const int GreedyPropertiesMayHaveData = 1;
    // Model contains at least one property that is expected to bind from value providers and a value provider has
    // matching data.
    internal const int ValueProviderDataAvailable = 2;

    private readonly IDictionary<ModelMetadata, IModelBinder> _propertyBinders;
    private readonly IReadOnlyList<IModelBinder> _parameterBinders;
    private readonly ILogger _logger;
    private Func<object>? _modelCreator;

    internal ComplexObjectModelBinder(
        IDictionary<ModelMetadata, IModelBinder> propertyBinders,
        IReadOnlyList<IModelBinder> parameterBinders,
        ILogger logger)
    {
        _propertyBinders = propertyBinders;
        _parameterBinders = parameterBinders;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        _logger.AttemptingToBindModel(bindingContext);

        var parameterData = CanCreateModel(bindingContext);
        if (parameterData == NoDataAvailable)
        {
            return Task.CompletedTask;
        }

        // Perf: separated to avoid allocating a state machine when we don't
        // need to go async.
        return BindModelCoreAsync(bindingContext, parameterData);
    }

    private async Task BindModelCoreAsync(ModelBindingContext bindingContext, int propertyData)
    {
        Debug.Assert(propertyData == GreedyPropertiesMayHaveData || propertyData == ValueProviderDataAvailable);

        // Create model first (if necessary) to avoid reporting errors about properties when activation fails.
        var attemptedBinding = false;
        var bindingSucceeded = false;

        var modelMetadata = bindingContext.ModelMetadata;
        var boundConstructor = modelMetadata.BoundConstructor;

        if (boundConstructor != null)
        {
            // Only record types are allowed to have a BoundConstructor. Binding a record type requires
            // instantiating the type. This means we'll ignore a previously assigned bindingContext.Model value.
            // This behaior is identical to input formatting with S.T.Json and Json.NET.

            var values = new object[boundConstructor.BoundConstructorParameters!.Count];
            var (attemptedParameterBinding, parameterBindingSucceeded) = await BindParametersAsync(
                bindingContext,
                propertyData,
                boundConstructor.BoundConstructorParameters,
                values);

            attemptedBinding |= attemptedParameterBinding;
            bindingSucceeded |= parameterBindingSucceeded;

            if (!CreateModel(bindingContext, boundConstructor, values))
            {
                return;
            }
        }
        else if (bindingContext.Model == null)
        {
            CreateModel(bindingContext);
        }

        var (attemptedPropertyBinding, propertyBindingSucceeded) = await BindPropertiesAsync(
            bindingContext,
            propertyData,
            modelMetadata.BoundProperties);

        attemptedBinding |= attemptedPropertyBinding;
        bindingSucceeded |= propertyBindingSucceeded;

        // Have we created a top-level model despite an inability to bind anything in said model and a lack of
        // other IsBindingRequired errors? Does that violate [BindRequired] on the model? This case occurs when
        // 1. The top-level model has no public settable properties.
        // 2. All properties in a [BindRequired] model have [BindNever] or are otherwise excluded from binding.
        // 3. No data exists for any property.
        if (!attemptedBinding &&
            bindingContext.IsTopLevelObject &&
            modelMetadata.IsBindingRequired)
        {
            var messageProvider = modelMetadata.ModelBindingMessageProvider;
            var message = messageProvider.MissingBindRequiredValueAccessor(bindingContext.FieldName);
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, message);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);

        // Have all binders failed because no data was available?
        //
        // If CanCreateModel determined a property has data, failures are likely due to conversion errors. For
        // example, user may submit ?[0].id=twenty&[1].id=twenty-one&[2].id=22 for a collection of a complex type
        // with an int id property. In that case, the bound model should be [ {}, {}, { id = 22 }] and
        // ModelState should contain errors about both [0].id and [1].id. Do not inform higher-level binders of the
        // failure in this and similar cases.
        //
        // If CanCreateModel could not find data for non-greedy properties, failures indicate greedy binders were
        // unsuccessful. For example, user may submit file attachments [0].File and [1].File but not [2].File for
        // a collection of a complex type containing an IFormFile property. In that case, we have exhausted the
        // attached files and checking for [3].File is likely be pointless. (And, if it had a point, would we stop
        // after 10 failures, 100, or more -- all adding redundant errors to ModelState?) Inform higher-level
        // binders of the failure.
        //
        // Required properties do not change the logic below. Missed required properties cause ModelState errors
        // but do not necessarily prevent further attempts to bind.
        //
        // This logic is intended to maximize correctness but does not avoid infinite loops or recursion when a
        // greedy model binder succeeds unconditionally.
        if (!bindingContext.IsTopLevelObject &&
            !bindingSucceeded &&
            propertyData == GreedyPropertiesMayHaveData)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
    }

    internal static bool CreateModel(ModelBindingContext bindingContext, ModelMetadata boundConstructor, object[] values)
    {
        try
        {
            bindingContext.Model = boundConstructor.BoundConstructorInvoker!(values);
            return true;
        }
        catch (Exception ex)
        {
            AddModelError(ex, bindingContext.ModelName, bindingContext);
            bindingContext.Result = ModelBindingResult.Failed();
            return false;
        }
    }

    /// <summary>
    /// Creates suitable <see cref="object"/> for given <paramref name="bindingContext"/>.
    /// </summary>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <returns>An <see cref="object"/> compatible with <see cref="ModelBindingContext.ModelType"/>.</returns>
    internal void CreateModel(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        // If model creator throws an exception, we want to propagate it back up the call stack, since the
        // application developer should know that this was an invalid type to try to bind to.
        if (_modelCreator == null)
        {
            // The following check causes the ComplexTypeModelBinder to NOT participate in binding structs as
            // reflection does not provide information about the implicit parameterless constructor for a struct.
            // This binder would eventually fail to construct an instance of the struct as the Linq's NewExpression
            // compile fails to construct it.
            var modelType = bindingContext.ModelType;
            if (modelType.IsAbstract || modelType.GetConstructor(Type.EmptyTypes) == null)
            {
                var metadata = bindingContext.ModelMetadata;
                switch (metadata.MetadataKind)
                {
                    case ModelMetadataKind.Parameter:
                        throw new InvalidOperationException(
                            Resources.FormatComplexObjectModelBinder_NoSuitableConstructor_ForParameter(
                                modelType.FullName,
                                metadata.ParameterName));
                    case ModelMetadataKind.Property:
                        throw new InvalidOperationException(
                            Resources.FormatComplexObjectModelBinder_NoSuitableConstructor_ForProperty(
                                modelType.FullName,
                                metadata.PropertyName,
                                bindingContext.ModelMetadata.ContainerType!.FullName));
                    case ModelMetadataKind.Type:
                        throw new InvalidOperationException(
                            Resources.FormatComplexObjectModelBinder_NoSuitableConstructor_ForType(
                                modelType.FullName));
                }
            }

            _modelCreator = Expression
                .Lambda<Func<object>>(Expression.New(bindingContext.ModelType))
                .Compile();
        }

        bindingContext.Model = _modelCreator();
    }

    private async ValueTask<(bool attemptedBinding, bool bindingSucceeded)> BindParametersAsync(
        ModelBindingContext bindingContext,
        int propertyData,
        IReadOnlyList<ModelMetadata> parameters,
        object?[] parameterValues)
    {
        var attemptedBinding = false;
        var bindingSucceeded = false;

        if (parameters.Count == 0)
        {
            return (attemptedBinding, bindingSucceeded);
        }

        var postponePlaceholderBinding = false;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            var fieldName = parameter.BinderModelName ?? parameter.ParameterName!;
            var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

            if (!CanBindItem(bindingContext, parameter))
            {
                continue;
            }

            var parameterBinder = _parameterBinders[i];
            if (parameterBinder is PlaceholderBinder)
            {
                if (postponePlaceholderBinding)
                {
                    // Decided to postpone binding properties that complete a loop in the model types when handling
                    // an earlier loop-completing property. Postpone binding this property too.
                    continue;
                }
                else if (!bindingContext.IsTopLevelObject &&
                    !bindingSucceeded &&
                    propertyData == GreedyPropertiesMayHaveData)
                {
                    // Have no confirmation of data for the current instance. Postpone completing the loop until
                    // we _know_ the current instance is useful. Recursion would otherwise occur prior to the
                    // block with a similar condition after the loop.
                    //
                    // Example cases include an Employee class containing
                    // 1. a Manager property of type Employee
                    // 2. an Employees property of type IList<Employee>
                    postponePlaceholderBinding = true;
                    continue;
                }
            }

            var result = await BindParameterAsync(bindingContext, parameter, parameterBinder, fieldName, modelName);

            if (result.IsModelSet)
            {
                attemptedBinding = true;
                bindingSucceeded = true;

                parameterValues[i] = result.Model;
            }
            else if (parameter.IsBindingRequired)
            {
                attemptedBinding = true;
            }
        }

        if (postponePlaceholderBinding && bindingSucceeded)
        {
            // Have some data for this instance. Continue with the model type loop.
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (!CanBindItem(bindingContext, parameter))
                {
                    continue;
                }

                var parameterBinder = _parameterBinders[i];
                if (parameterBinder is PlaceholderBinder)
                {
                    var fieldName = parameter.BinderModelName ?? parameter.ParameterName!;
                    var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

                    var result = await BindParameterAsync(bindingContext, parameter, parameterBinder, fieldName, modelName);

                    if (result.IsModelSet)
                    {
                        parameterValues[i] = result.Model;
                    }
                }
            }
        }

        return (attemptedBinding, bindingSucceeded);
    }

    private async ValueTask<(bool attemptedBinding, bool bindingSucceeded)> BindPropertiesAsync(
        ModelBindingContext bindingContext,
        int propertyData,
        IReadOnlyList<ModelMetadata> boundProperties)
    {
        var attemptedBinding = false;
        var bindingSucceeded = false;

        if (boundProperties.Count == 0)
        {
            return (attemptedBinding, bindingSucceeded);
        }

        var postponePlaceholderBinding = false;
        for (var i = 0; i < boundProperties.Count; i++)
        {
            var property = boundProperties[i];
            if (!CanBindItem(bindingContext, property))
            {
                continue;
            }

            var propertyBinder = _propertyBinders[property];
            if (propertyBinder is PlaceholderBinder)
            {
                if (postponePlaceholderBinding)
                {
                    // Decided to postpone binding properties that complete a loop in the model types when handling
                    // an earlier loop-completing property. Postpone binding this property too.
                    continue;
                }
                else if (!bindingContext.IsTopLevelObject &&
                    !bindingSucceeded &&
                    propertyData == GreedyPropertiesMayHaveData)
                {
                    // Have no confirmation of data for the current instance. Postpone completing the loop until
                    // we _know_ the current instance is useful. Recursion would otherwise occur prior to the
                    // block with a similar condition after the loop.
                    //
                    // Example cases include an Employee class containing
                    // 1. a Manager property of type Employee
                    // 2. an Employees property of type IList<Employee>
                    postponePlaceholderBinding = true;
                    continue;
                }
            }

            var fieldName = property.BinderModelName ?? property.PropertyName!;
            var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
            var result = await BindPropertyAsync(bindingContext, property, propertyBinder, fieldName, modelName);

            if (result.IsModelSet)
            {
                attemptedBinding = true;
                bindingSucceeded = true;
            }
            else if (property.IsBindingRequired)
            {
                attemptedBinding = true;
            }
        }

        if (postponePlaceholderBinding && bindingSucceeded)
        {
            // Have some data for this instance. Continue with the model type loop.
            for (var i = 0; i < boundProperties.Count; i++)
            {
                var property = boundProperties[i];
                if (!CanBindItem(bindingContext, property))
                {
                    continue;
                }

                var propertyBinder = _propertyBinders[property];
                if (propertyBinder is PlaceholderBinder)
                {
                    var fieldName = property.BinderModelName ?? property.PropertyName!;
                    var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);

                    await BindPropertyAsync(bindingContext, property, propertyBinder, fieldName, modelName);
                }
            }
        }

        return (attemptedBinding, bindingSucceeded);
    }

    internal static bool CanBindItem(ModelBindingContext bindingContext, ModelMetadata propertyMetadata)
    {
        var metadataProviderFilter = bindingContext.ModelMetadata.PropertyFilterProvider?.PropertyFilter;
        if (metadataProviderFilter?.Invoke(propertyMetadata) == false)
        {
            return false;
        }

        if (bindingContext.PropertyFilter?.Invoke(propertyMetadata) == false)
        {
            return false;
        }

        if (!propertyMetadata.IsBindingAllowed)
        {
            return false;
        }

        if (propertyMetadata.MetadataKind == ModelMetadataKind.Property && propertyMetadata.IsReadOnly)
        {
            // Determine if we can update a readonly property (such as a collection).
            return CanUpdateReadOnlyProperty(propertyMetadata.ModelType);
        }

        return true;
    }

    private static async ValueTask<ModelBindingResult> BindPropertyAsync(
        ModelBindingContext bindingContext,
        ModelMetadata property,
        IModelBinder propertyBinder,
        string fieldName,
        string modelName)
    {
        Debug.Assert(property.MetadataKind == ModelMetadataKind.Property);

        // Pass complex (including collection) values down so that binding system does not unnecessarily
        // recreate instances or overwrite inner properties that are not bound. No need for this with simple
        // values because they will be overwritten if binding succeeds. Arrays are never reused because they
        // cannot be resized.
        object? propertyModel = null;
        if (property.PropertyGetter != null &&
            property.IsComplexType &&
            !property.ModelType.IsArray)
        {
            propertyModel = property.PropertyGetter(bindingContext.Model!);
        }

        ModelBindingResult result;
        using (bindingContext.EnterNestedScope(
            modelMetadata: property,
            fieldName: fieldName,
            modelName: modelName,
            model: propertyModel))
        {
            await propertyBinder.BindModelAsync(bindingContext);
            result = bindingContext.Result;
        }

        if (result.IsModelSet)
        {
            SetProperty(bindingContext, modelName, property, result);
        }
        else if (property.IsBindingRequired)
        {
            var message = property.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
            bindingContext.ModelState.TryAddModelError(modelName, message);
        }

        return result;
    }

    private static async ValueTask<ModelBindingResult> BindParameterAsync(
        ModelBindingContext bindingContext,
        ModelMetadata parameter,
        IModelBinder parameterBinder,
        string fieldName,
        string modelName)
    {
        Debug.Assert(parameter.MetadataKind == ModelMetadataKind.Parameter);

        ModelBindingResult result;
        using (bindingContext.EnterNestedScope(
            modelMetadata: parameter,
            fieldName: fieldName,
            modelName: modelName,
            model: null))
        {
            await parameterBinder.BindModelAsync(bindingContext);
            result = bindingContext.Result;
        }

        if (!result.IsModelSet && parameter.IsBindingRequired)
        {
            var message = parameter.ModelBindingMessageProvider.MissingBindRequiredValueAccessor(fieldName);
            bindingContext.ModelState.TryAddModelError(modelName, message);
        }

        return result;
    }

    internal int CanCreateModel(ModelBindingContext bindingContext)
    {
        var isTopLevelObject = bindingContext.IsTopLevelObject;

        // If we get here the model is a complex object which was not directly bound by any previous model binder,
        // so we want to decide if we want to continue binding. This is important to get right to avoid infinite
        // recursion.
        //
        // First, we want to make sure this object is allowed to come from a value provider source as this binder
        // will only include value provider data. For instance if the model is marked with [FromBody], then we
        // can just skip it. A greedy source cannot be a value provider.
        //
        // If the model isn't marked with ANY binding source, then we assume it's OK also.
        //
        // We skip this check if it is a top level object because we want to always evaluate
        // the creation of top level object (this is also required for ModelBinderAttribute to work.)
        var bindingSource = bindingContext.BindingSource;
        if (!isTopLevelObject && bindingSource != null && bindingSource.IsGreedy)
        {
            return NoDataAvailable;
        }

        // Create the object if:
        // 1. It is a top level model.
        if (isTopLevelObject)
        {
            return ValueProviderDataAvailable;
        }

        // 2. Any of the model properties can be bound.
        return CanBindAnyModelItem(bindingContext);
    }

    private int CanBindAnyModelItem(ModelBindingContext bindingContext)
    {
        // If there are no properties on the model, and no constructor parameters, there is nothing to bind. We are here means this is not a top
        // level object. So we return false.
        var modelMetadata = bindingContext.ModelMetadata;
        var performsConstructorBinding = bindingContext.Model == null && modelMetadata.BoundConstructor != null;

        if (modelMetadata.Properties.Count == 0 &&
             (!performsConstructorBinding || modelMetadata.BoundConstructor!.BoundConstructorParameters!.Count == 0))
        {
            Log.NoPublicSettableItems(_logger, bindingContext);
            return NoDataAvailable;
        }

        // We want to check to see if any of the properties of the model can be bound using the value providers or
        // a greedy binder.
        //
        // Because a property might specify a custom binding source ([FromForm]), it's not correct
        // for us to just try bindingContext.ValueProvider.ContainsPrefixAsync(bindingContext.ModelName);
        // that may include other value providers - that would lead us to mistakenly create the model
        // when the data is coming from a source we should use (ex: value found in query string, but the
        // model has [FromForm]).
        //
        // To do this we need to enumerate the properties, and see which of them provide a binding source
        // through metadata, then we decide what to do.
        //
        //      If a property has a binding source, and it's a greedy source, then it's always bound.
        //
        //      If a property has a binding source, and it's a non-greedy source, then we'll filter the
        //      the value providers to just that source, and see if we can find a matching prefix
        //      (see CanBindValue).
        //
        //      If a property does not have a binding source, then it's fair game for any value provider.
        //
        // Bottom line, if any property meets the above conditions and has a value from ValueProviders, then we'll
        // create the model and try to bind it. Of, if ANY properties of the model have a greedy source,
        // then we go ahead and create it.
        var hasGreedyBinders = false;
        for (var i = 0; i < bindingContext.ModelMetadata.Properties.Count; i++)
        {
            var propertyMetadata = bindingContext.ModelMetadata.Properties[i];
            if (!CanBindItem(bindingContext, propertyMetadata))
            {
                continue;
            }

            // If any property can be bound from a greedy binding source, then success.
            var bindingSource = propertyMetadata.BindingSource;
            if (bindingSource != null && bindingSource.IsGreedy)
            {
                hasGreedyBinders = true;
                continue;
            }

            // Otherwise, check whether the (perhaps filtered) value providers have a match.
            var fieldName = propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName!;
            var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
            using (bindingContext.EnterNestedScope(
                modelMetadata: propertyMetadata,
                fieldName: fieldName,
                modelName: modelName,
                model: null))
            {
                // If any property can be bound from a value provider, then success.
                if (bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
                {
                    return ValueProviderDataAvailable;
                }
            }
        }

        if (performsConstructorBinding)
        {
            var parameters = bindingContext.ModelMetadata.BoundConstructor!.BoundConstructorParameters!;
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameterMetadata = parameters[i];
                if (!CanBindItem(bindingContext, parameterMetadata))
                {
                    continue;
                }

                // If any parameter can be bound from a greedy binding source, then success.
                var bindingSource = parameterMetadata.BindingSource;
                if (bindingSource != null && bindingSource.IsGreedy)
                {
                    hasGreedyBinders = true;
                    continue;
                }

                // Otherwise, check whether the (perhaps filtered) value providers have a match.
                var fieldName = parameterMetadata.BinderModelName ?? parameterMetadata.ParameterName!;
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                using (bindingContext.EnterNestedScope(
                    modelMetadata: parameterMetadata,
                    fieldName: fieldName,
                    modelName: modelName,
                    model: null))
                {
                    // If any parameter can be bound from a value provider, then success.
                    if (bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
                    {
                        return ValueProviderDataAvailable;
                    }
                }
            }
        }

        if (hasGreedyBinders)
        {
            return GreedyPropertiesMayHaveData;
        }

        Log.CannotBindToComplexType(_logger, bindingContext);

        return NoDataAvailable;
    }

    internal static bool CanUpdateReadOnlyProperty(Type propertyType)
    {
        // Value types have copy-by-value semantics, which prevents us from updating
        // properties that are marked readonly.
        if (propertyType.IsValueType)
        {
            return false;
        }

        // Arrays are strange beasts since their contents are mutable but their sizes aren't.
        // Therefore we shouldn't even try to update these. Further reading:
        // http://blogs.msdn.com/ericlippert/archive/2008/09/22/arrays-considered-somewhat-harmful.aspx
        if (propertyType.IsArray)
        {
            return false;
        }

        // Special-case known immutable reference types
        if (propertyType == typeof(string))
        {
            return false;
        }

        return true;
    }

    internal static void SetProperty(
        ModelBindingContext bindingContext,
        string modelName,
        ModelMetadata propertyMetadata,
        ModelBindingResult result)
    {
        if (!result.IsModelSet)
        {
            // If we don't have a value, don't set it on the model and trounce a pre-initialized value.
            return;
        }

        if (propertyMetadata.IsReadOnly)
        {
            // The property should have already been set when we called BindPropertyAsync, so there's
            // nothing to do here.
            return;
        }

        var value = result.Model;
        try
        {
            propertyMetadata.PropertySetter!(bindingContext.Model!, value);
        }
        catch (Exception exception)
        {
            AddModelError(exception, modelName, bindingContext);
        }
    }

    private static void AddModelError(
        Exception exception,
        string modelName,
        ModelBindingContext bindingContext)
    {
        var targetInvocationException = exception as TargetInvocationException;
        if (targetInvocationException?.InnerException != null)
        {
            exception = targetInvocationException.InnerException;
        }

        // Do not add an error message if a binding error has already occurred for this property.
        var modelState = bindingContext.ModelState;
        var validationState = modelState.GetFieldValidationState(modelName);
        if (validationState == ModelValidationState.Unvalidated)
        {
            modelState.AddModelError(modelName, exception, bindingContext.ModelMetadata);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(17, LogLevel.Debug, "Could not bind to model with name '{ModelName}' and type '{ModelType}' as the type has no " +
            "public settable properties or constructor parameters.", EventName = "NoPublicSettableItems")]
        public static partial void NoPublicSettableItems(ILogger logger, string modelName, Type modelType);

        public static void NoPublicSettableItems(ILogger logger, ModelBindingContext bindingContext)
        {
            NoPublicSettableItems(logger, bindingContext.ModelName, bindingContext.ModelType);
        }

        public static void CannotBindToComplexType(ILogger logger, ModelBindingContext bindingContext)
            => CannotBindToComplexType(logger, bindingContext.ModelType);

        [LoggerMessage(18, LogLevel.Debug, "Could not bind to model of type '{ModelType}' as there were no values in the request for any of the properties.", EventName = "CannotBindToComplexType")]
        private static partial void CannotBindToComplexType(ILogger logger, Type modelType);
    }
}
