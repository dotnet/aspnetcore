// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Binding info which represents metadata associated to an action parameter.
/// </summary>
public class BindingInfo
{
    private Type? _binderType;

    /// <summary>
    /// Creates a new <see cref="BindingInfo"/>.
    /// </summary>
    public BindingInfo()
    {
    }

    /// <summary>
    /// Creates a copy of a <see cref="BindingInfo"/>.
    /// </summary>
    /// <param name="other">The <see cref="BindingInfo"/> to copy.</param>
    public BindingInfo(BindingInfo other)
    {
        ArgumentNullException.ThrowIfNull(other);

        BindingSource = other.BindingSource;
        BinderModelName = other.BinderModelName;
        BinderType = other.BinderType;
        PropertyFilterProvider = other.PropertyFilterProvider;
        RequestPredicate = other.RequestPredicate;
        EmptyBodyBehavior = other.EmptyBodyBehavior;
        ServiceKey = other.ServiceKey;
    }

    /// <summary>
    /// Gets or sets the <see cref="ModelBinding.BindingSource"/>.
    /// </summary>
    public BindingSource? BindingSource { get; set; }

    /// <summary>
    /// Gets or sets the binder model name.
    /// </summary>
    public string? BinderModelName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Type"/> of the <see cref="IModelBinder"/> implementation used to bind the
    /// model.
    /// </summary>
    /// <remarks>
    /// Also set <see cref="BindingSource"/> if the specified <see cref="IModelBinder"/> implementation does not
    /// use values from form data, route values or the query string.
    /// </remarks>
    public Type? BinderType
    {
        get => _binderType;
        set
        {
            if (value != null && !typeof(IModelBinder).IsAssignableFrom(value))
            {
                throw new ArgumentException(
                    Resources.FormatBinderType_MustBeIModelBinder(
                        value.FullName,
                        typeof(IModelBinder).FullName),
                    nameof(value));
            }

            _binderType = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="ModelBinding.IPropertyFilterProvider"/>.
    /// </summary>
    public IPropertyFilterProvider? PropertyFilterProvider { get; set; }

    /// <summary>
    /// Gets or sets a predicate which determines whether or not the model should be bound based on state
    /// from the current request.
    /// </summary>
    public Func<ActionContext, bool>? RequestPredicate { get; set; }

    /// <summary>
    /// Gets or sets the value which decides if empty bodies are treated as valid inputs.
    /// </summary>
    public EmptyBodyBehavior EmptyBodyBehavior { get; set; }

    /// <summary>
    /// Get or sets the value used as the key when looking for a keyed service
    /// </summary>
    public object? ServiceKey { get; set; }

    /// <summary>
    /// Constructs a new instance of <see cref="BindingInfo"/> from the given <paramref name="attributes"/>.
    /// <para>
    /// This overload does not account for <see cref="BindingInfo"/> specified via <see cref="ModelMetadata"/>. Consider using
    /// <see cref="GetBindingInfo(IEnumerable{object}, ModelMetadata)"/> overload, or <see cref="TryApplyBindingInfo(ModelMetadata)"/>
    /// on the result of this method to get a more accurate <see cref="BindingInfo"/> instance.
    /// </para>
    /// </summary>
    /// <param name="attributes">A collection of attributes which are used to construct <see cref="BindingInfo"/>
    /// </param>
    /// <returns>A new instance of <see cref="BindingInfo"/>.</returns>
    public static BindingInfo? GetBindingInfo(IEnumerable<object> attributes)
    {
        var bindingInfo = new BindingInfo();
        var isBindingInfoPresent = false;

        // BinderModelName
        foreach (var binderModelNameAttribute in attributes.OfType<IModelNameProvider>())
        {
            isBindingInfoPresent = true;
            if (binderModelNameAttribute?.Name != null)
            {
                bindingInfo.BinderModelName = binderModelNameAttribute.Name;
                break;
            }
        }

        // BinderType
        foreach (var binderTypeAttribute in attributes.OfType<IBinderTypeProviderMetadata>())
        {
            isBindingInfoPresent = true;
            if (binderTypeAttribute.BinderType != null)
            {
                bindingInfo.BinderType = binderTypeAttribute.BinderType;
                break;
            }
        }

        // BindingSource
        foreach (var bindingSourceAttribute in attributes.OfType<IBindingSourceMetadata>())
        {
            isBindingInfoPresent = true;
            if (bindingSourceAttribute.BindingSource != null)
            {
                bindingInfo.BindingSource = bindingSourceAttribute.BindingSource;
                break;
            }
        }

        // PropertyFilterProvider
        var propertyFilterProviders = attributes.OfType<IPropertyFilterProvider>().ToArray();
        if (propertyFilterProviders.Length == 1)
        {
            isBindingInfoPresent = true;
            bindingInfo.PropertyFilterProvider = propertyFilterProviders[0];
        }
        else if (propertyFilterProviders.Length > 1)
        {
            isBindingInfoPresent = true;
            bindingInfo.PropertyFilterProvider = new CompositePropertyFilterProvider(propertyFilterProviders);
        }

        // RequestPredicate
        foreach (var requestPredicateProvider in attributes.OfType<IRequestPredicateProvider>())
        {
            isBindingInfoPresent = true;
            if (requestPredicateProvider.RequestPredicate != null)
            {
                bindingInfo.RequestPredicate = requestPredicateProvider.RequestPredicate;
                break;
            }
        }

        foreach (var configureEmptyBodyBehavior in attributes.OfType<IConfigureEmptyBodyBehavior>())
        {
            isBindingInfoPresent = true;
            bindingInfo.EmptyBodyBehavior = configureEmptyBodyBehavior.EmptyBodyBehavior;
            break;
        }

        // Keyed services
        if (attributes.OfType<FromKeyedServicesAttribute>().FirstOrDefault() is { } fromKeyedServicesAttribute)
        {
            if (bindingInfo.BindingSource != null)
            {
                throw new NotSupportedException(
                    $"The {nameof(FromKeyedServicesAttribute)} is not supported on parameters that are also annotated with {nameof(IBindingSourceMetadata)}.");
            }
            isBindingInfoPresent = true;
            bindingInfo.BindingSource = BindingSource.Services;
            bindingInfo.ServiceKey = fromKeyedServicesAttribute.Key;
        }

        return isBindingInfoPresent ? bindingInfo : null;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="BindingInfo"/> from the given <paramref name="attributes"/> and <paramref name="modelMetadata"/>.
    /// </summary>
    /// <param name="attributes">A collection of attributes which are used to construct <see cref="BindingInfo"/>.</param>
    /// <param name="modelMetadata">The <see cref="ModelMetadata"/>.</param>
    /// <returns>A new instance of <see cref="BindingInfo"/> if any binding metadata was discovered; otherwise or <see langword="null"/>.</returns>
    public static BindingInfo? GetBindingInfo(IEnumerable<object> attributes, ModelMetadata modelMetadata)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(modelMetadata);

        var bindingInfo = GetBindingInfo(attributes);
        var isBindingInfoPresent = bindingInfo != null;

        if (bindingInfo == null)
        {
            bindingInfo = new BindingInfo();
        }

        isBindingInfoPresent |= bindingInfo.TryApplyBindingInfo(modelMetadata);

        return isBindingInfoPresent ? bindingInfo : null;
    }

    /// <summary>
    /// Applies binding metadata from the specified <paramref name="modelMetadata"/>.
    /// <para>
    /// Uses values from <paramref name="modelMetadata"/> if no value is already available.
    /// </para>
    /// </summary>
    /// <param name="modelMetadata">The <see cref="ModelMetadata"/>.</param>
    /// <returns><see langword="true"/> if any binding metadata from <paramref name="modelMetadata"/> was applied;
    /// <see langword="false"/> otherwise.</returns>
    public bool TryApplyBindingInfo(ModelMetadata modelMetadata)
    {
        ArgumentNullException.ThrowIfNull(modelMetadata);

        var isBindingInfoPresent = false;
        if (BinderModelName == null && modelMetadata.BinderModelName != null)
        {
            isBindingInfoPresent = true;
            BinderModelName = modelMetadata.BinderModelName;
        }

        if (BinderType == null && modelMetadata.BinderType != null)
        {
            isBindingInfoPresent = true;
            BinderType = modelMetadata.BinderType;
        }

        if (BindingSource == null && modelMetadata.BindingSource != null)
        {
            isBindingInfoPresent = true;
            BindingSource = modelMetadata.BindingSource;
        }

        if (PropertyFilterProvider == null && modelMetadata.PropertyFilterProvider != null)
        {
            isBindingInfoPresent = true;
            PropertyFilterProvider = modelMetadata.PropertyFilterProvider;
        }

        // If the EmptyBody behavior is not configured will be inferred
        // as Allow when the NullablityState == NullablityStateNull or HasDefaultValue
        // https://github.com/dotnet/aspnetcore/issues/39754
        if (EmptyBodyBehavior == EmptyBodyBehavior.Default &&
            BindingSource == BindingSource.Body &&
            (modelMetadata.NullabilityState == NullabilityState.Nullable || modelMetadata.IsNullableValueType || modelMetadata.HasDefaultValue))
        {
            isBindingInfoPresent = true;
            EmptyBodyBehavior = EmptyBodyBehavior.Allow;
        }

        return isBindingInfoPresent;
    }

    private sealed class CompositePropertyFilterProvider : IPropertyFilterProvider
    {
        private readonly IEnumerable<IPropertyFilterProvider> _providers;

        public CompositePropertyFilterProvider(IEnumerable<IPropertyFilterProvider> providers)
        {
            _providers = providers;
        }

        public Func<ModelMetadata, bool> PropertyFilter => CreatePropertyFilter();

        private Func<ModelMetadata, bool> CreatePropertyFilter()
        {
            var propertyFilters = _providers
                .Select(p => p.PropertyFilter)
                .Where(p => p != null);

            return (m) =>
            {
                foreach (var propertyFilter in propertyFilters)
                {
                    if (!propertyFilter(m))
                    {
                        return false;
                    }
                }

                return true;
            };
        }
    }
}
