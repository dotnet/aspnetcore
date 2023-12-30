// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A context that contains operating information for model binding and validation.
/// </summary>
public class DefaultModelBindingContext : ModelBindingContext
{
    private static readonly IValueProvider EmptyValueProvider = new CompositeValueProvider();

    private IValueProvider _originalValueProvider = default!;
    private ActionContext _actionContext = default!;
    private ModelStateDictionary _modelState = default!;
    private ValidationStateDictionary _validationState = default!;
    private int? _maxModelBindingRecursionDepth;

    private State _state;
    private readonly Stack<State> _stack = new Stack<State>();

    /// <inheritdoc />
    public override ActionContext ActionContext
    {
        get { return _actionContext; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _actionContext = value;
        }
    }

    /// <inheritdoc />
    public override string FieldName
    {
        get { return _state.FieldName; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _state.FieldName = value;
        }
    }

    /// <inheritdoc />
    public override object? Model
    {
        get { return _state.Model; }
        set { _state.Model = value; }
    }

    /// <inheritdoc />
    public override ModelMetadata ModelMetadata
    {
        get { return _state.ModelMetadata; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _state.ModelMetadata = value;
        }
    }

    /// <inheritdoc />
    public override string ModelName
    {
        get { return _state.ModelName; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _state.ModelName = value;
        }
    }

    /// <inheritdoc />
    public override ModelStateDictionary ModelState
    {
        get { return _modelState; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _modelState = value;
        }
    }

    /// <inheritdoc />
    public override string? BinderModelName
    {
        get => _state.BinderModelName;
        set => _state.BinderModelName = value;
    }

    /// <inheritdoc />
    public override BindingSource? BindingSource
    {
        get { return _state.BindingSource; }
        set { _state.BindingSource = value; }
    }

    /// <inheritdoc />
    public override bool IsTopLevelObject
    {
        get { return _state.IsTopLevelObject; }
        set { _state.IsTopLevelObject = value; }
    }

    /// <summary>
    /// Gets or sets the original value provider to be used when value providers are not filtered.
    /// </summary>
    public IValueProvider OriginalValueProvider
    {
        get { return _originalValueProvider; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _originalValueProvider = value;
        }
    }

    /// <inheritdoc />
    public override IValueProvider ValueProvider
    {
        get { return _state.ValueProvider; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _state.ValueProvider = value;
        }
    }

    /// <inheritdoc />
    public override Func<ModelMetadata, bool>? PropertyFilter
    {
        get { return _state.PropertyFilter; }
        set { _state.PropertyFilter = value; }
    }

    /// <inheritdoc />
    public override ValidationStateDictionary ValidationState
    {
        get { return _validationState; }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _validationState = value;
        }
    }

    /// <inheritdoc />
    public override ModelBindingResult Result
    {
        get
        {
            return _state.Result;
        }
        set
        {
            _state.Result = value;
        }
    }

    private int MaxModelBindingRecursionDepth
    {
        get
        {
            if (!_maxModelBindingRecursionDepth.HasValue)
            {
                // Ignore incomplete initialization. This must be a test scenario because CreateBindingContext(...)
                // has not been called or was called without MvcOptions in the service provider.
                _maxModelBindingRecursionDepth = MvcOptions.DefaultMaxModelBindingRecursionDepth;
            }

            return _maxModelBindingRecursionDepth.Value;
        }
        set
        {
            _maxModelBindingRecursionDepth = value;
        }
    }

    /// <summary>
    /// Creates a new <see cref="DefaultModelBindingContext"/> for top-level model binding operation.
    /// </summary>
    /// <param name="actionContext">
    /// The <see cref="ActionContext"/> associated with the binding operation.
    /// </param>
    /// <param name="valueProvider">The <see cref="IValueProvider"/> to use for binding.</param>
    /// <param name="metadata"><see cref="ModelMetadata"/> associated with the model.</param>
    /// <param name="bindingInfo"><see cref="BindingInfo"/> associated with the model.</param>
    /// <param name="modelName">The name of the property or parameter being bound.</param>
    /// <returns>A new instance of <see cref="DefaultModelBindingContext"/>.</returns>
    public static ModelBindingContext CreateBindingContext(
        ActionContext actionContext,
        IValueProvider valueProvider,
        ModelMetadata metadata,
        BindingInfo? bindingInfo,
        string modelName)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentNullException.ThrowIfNull(valueProvider);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(modelName);

        var binderModelName = bindingInfo?.BinderModelName ?? metadata.BinderModelName;
        var bindingSource = bindingInfo?.BindingSource ?? metadata.BindingSource;
        var propertyFilterProvider = bindingInfo?.PropertyFilterProvider ?? metadata.PropertyFilterProvider;

        var bindingContext = new DefaultModelBindingContext()
        {
            ActionContext = actionContext,
            BinderModelName = binderModelName,
            BindingSource = bindingSource,
            PropertyFilter = propertyFilterProvider?.PropertyFilter,
            ValidationState = new ValidationStateDictionary(),

            // Because this is the top-level context, FieldName and ModelName should be the same.
            FieldName = binderModelName ?? modelName,
            ModelName = binderModelName ?? modelName,
            OriginalModelName = binderModelName ?? modelName,

            IsTopLevelObject = true,
            ModelMetadata = metadata,
            ModelState = actionContext.ModelState,

            OriginalValueProvider = valueProvider,
            ValueProvider = FilterValueProvider(valueProvider, bindingSource),
        };

        // mvcOptions may be null when this method is called in test scenarios.
        var mvcOptions = actionContext.HttpContext.RequestServices?.GetService<IOptions<MvcOptions>>();
        if (mvcOptions != null)
        {
            bindingContext.MaxModelBindingRecursionDepth = mvcOptions.Value.MaxModelBindingRecursionDepth;
        }

        return bindingContext;
    }

    /// <inheritdoc />
    public override NestedScope EnterNestedScope(
        ModelMetadata modelMetadata,
        string fieldName,
        string modelName,
        object? model)
    {
        ArgumentNullException.ThrowIfNull(modelMetadata);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(modelName);

        var scope = EnterNestedScope();

        // Only filter if the new BindingSource affects the value providers. Otherwise we want
        // to preserve the current state.
        if (modelMetadata.BindingSource != null && !modelMetadata.BindingSource.IsGreedy)
        {
            ValueProvider = FilterValueProvider(OriginalValueProvider, modelMetadata.BindingSource);
        }

        Model = model;
        ModelMetadata = modelMetadata;
        ModelName = modelName;
        FieldName = fieldName;
        BinderModelName = modelMetadata.BinderModelName;
        BindingSource = modelMetadata.BindingSource;
        PropertyFilter = modelMetadata.PropertyFilterProvider?.PropertyFilter;

        IsTopLevelObject = false;

        return scope;
    }

    /// <inheritdoc />
    public override NestedScope EnterNestedScope()
    {
        _stack.Push(_state);

        // Would this new scope (which isn't in _stack) exceed the allowed recursion depth? That is, has the model
        // binding system already nested MaxModelBindingRecursionDepth binders?
        if (_stack.Count >= MaxModelBindingRecursionDepth)
        {
            // Find the root of this deeply-nested model.
            var states = _stack.ToArray();
            var rootModelType = states[states.Length - 1].ModelMetadata.ModelType;

            throw new InvalidOperationException(Resources.FormatModelBinding_ExceededMaxModelBindingRecursionDepth(
                nameof(MvcOptions),
                nameof(MvcOptions.MaxModelBindingRecursionDepth),
                MaxModelBindingRecursionDepth,
                rootModelType));
        }

        Result = default;

        return new NestedScope(this);
    }

    /// <inheritdoc />
    protected override void ExitNestedScope()
    {
        _state = _stack.Pop();
    }

    private static IValueProvider FilterValueProvider(IValueProvider valueProvider, BindingSource? bindingSource)
    {
        if (bindingSource == null || bindingSource.IsGreedy)
        {
            return valueProvider;
        }

        if (valueProvider is not IBindingSourceValueProvider bindingSourceValueProvider)
        {
            return valueProvider;
        }

        return bindingSourceValueProvider.Filter(bindingSource) ?? EmptyValueProvider;
    }

    private struct State
    {
        public string FieldName;
        public object? Model;
        public ModelMetadata ModelMetadata;
        public string ModelName;

        public IValueProvider ValueProvider;
        public Func<ModelMetadata, bool>? PropertyFilter;

        public string? BinderModelName;
        public BindingSource? BindingSource;
        public bool IsTopLevelObject;

        public ModelBindingResult Result;
    }
}
