// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A default implementation of <see cref="IBindingMetadataProvider"/>.
/// </summary>
internal sealed class DefaultBindingMetadataProvider : IBindingMetadataProvider
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // BinderModelName
        foreach (var binderModelNameAttribute in context.Attributes.OfType<IModelNameProvider>())
        {
            if (binderModelNameAttribute.Name != null)
            {
                context.BindingMetadata.BinderModelName = binderModelNameAttribute.Name;
                break;
            }
        }

        // BinderType
        foreach (var binderTypeAttribute in context.Attributes.OfType<IBinderTypeProviderMetadata>())
        {
            if (binderTypeAttribute.BinderType != null)
            {
                context.BindingMetadata.BinderType = binderTypeAttribute.BinderType;
                break;
            }
        }

        // BindingSource
        foreach (var bindingSourceAttribute in context.Attributes.OfType<IBindingSourceMetadata>())
        {
            if (bindingSourceAttribute.BindingSource != null)
            {
                context.BindingMetadata.BindingSource = bindingSourceAttribute.BindingSource;
                break;
            }
        }

        // PropertyFilterProvider
        var propertyFilterProviders = context.Attributes.OfType<IPropertyFilterProvider>().ToArray();
        if (propertyFilterProviders.Length == 0)
        {
            context.BindingMetadata.PropertyFilterProvider = null;
        }
        else if (propertyFilterProviders.Length == 1)
        {
            context.BindingMetadata.PropertyFilterProvider = propertyFilterProviders[0];
        }
        else
        {
            var composite = new CompositePropertyFilterProvider(propertyFilterProviders);
            context.BindingMetadata.PropertyFilterProvider = composite;
        }

        var bindingBehavior = FindBindingBehavior(context);
        if (bindingBehavior != null)
        {
            context.BindingMetadata.IsBindingAllowed = bindingBehavior.Behavior != BindingBehavior.Never;
            context.BindingMetadata.IsBindingRequired = bindingBehavior.Behavior == BindingBehavior.Required;
        }

        if (GetBoundConstructor(context.Key.ModelType) is ConstructorInfo constructorInfo)
        {
            context.BindingMetadata.BoundConstructor = constructorInfo;
        }
    }

    internal static ConstructorInfo? GetBoundConstructor(Type type)
    {
        if (type.IsAbstract || type.IsValueType || type.IsInterface)
        {
            return null;
        }

        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
        {
            return null;
        }

        return GetRecordTypeConstructor(type, constructors);
    }

    private static ConstructorInfo? GetRecordTypeConstructor(Type type, ConstructorInfo[] constructors)
    {
        if (!IsRecordType(type))
        {
            return null;
        }

        // For record types, we will support binding and validating the primary constructor.
        // There isn't metadata to identify a primary constructor. Our heuristic is:
        // We require exactly one constructor to be defined on the type, and that every parameter on
        // that constructor is mapped to a property with the same name and type.

        if (constructors.Length > 1)
        {
            return null;
        }

        var constructor = constructors[0];

        var parameters = constructor.GetParameters();
        if (parameters.Length == 0)
        {
            // We do not need to do special handling for parameterless constructors.
            return null;
        }

        var properties = PropertyHelper.GetVisibleProperties(type);

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var mappedProperty = properties.FirstOrDefault(property =>
                string.Equals(property.Name, parameter.Name, StringComparison.Ordinal) &&
                property.Property.PropertyType == parameter.ParameterType);

            if (mappedProperty is null)
            {
                // No property found, this is not a primary constructor.
                return null;
            }
        }

        return constructor;

        static bool IsRecordType(Type type)
        {
            // Based on the state of the art as described in https://github.com/dotnet/roslyn/issues/45777
            var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
            return cloneMethod != null && (cloneMethod.ReturnType == type || cloneMethod.ReturnType == type.BaseType);
        }
    }

    private static BindingBehaviorAttribute? FindBindingBehavior(BindingMetadataProviderContext context)
    {
        switch (context.Key.MetadataKind)
        {
            case ModelMetadataKind.Property:
                // BindingBehavior can fall back to attributes on the Container Type, but we should ignore
                // attributes on the Property Type.
                var matchingAttributes = context.PropertyAttributes!.OfType<BindingBehaviorAttribute>();
                return matchingAttributes.FirstOrDefault()
                    ?? context.Key.ContainerType!
                        .GetCustomAttributes(typeof(BindingBehaviorAttribute), inherit: true)
                        .OfType<BindingBehaviorAttribute>()
                        .FirstOrDefault();
            case ModelMetadataKind.Parameter:
                return context.ParameterAttributes!.OfType<BindingBehaviorAttribute>().FirstOrDefault();
            default:
                return null;
        }
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
