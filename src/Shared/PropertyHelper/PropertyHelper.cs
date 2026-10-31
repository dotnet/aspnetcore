// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

[assembly: MetadataUpdateHandler(typeof(Microsoft.Extensions.Internal.PropertyHelper.MetadataUpdateHandler))]

namespace Microsoft.Extensions.Internal;

internal sealed class PropertyHelper
{
    private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags Everything = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

    // Delegate type for a by-ref property getter
    private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

    private static readonly MethodInfo CallPropertyGetterOpenGenericMethod =
        typeof(PropertyHelper).GetMethod(nameof(CallPropertyGetter), DeclaredOnlyLookup)!;

    private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod =
        typeof(PropertyHelper).GetMethod(nameof(CallPropertyGetterByReference), DeclaredOnlyLookup)!;

    private static readonly MethodInfo CallNullSafePropertyGetterOpenGenericMethod =
        typeof(PropertyHelper).GetMethod(nameof(CallNullSafePropertyGetter), DeclaredOnlyLookup)!;

    private static readonly MethodInfo CallNullSafePropertyGetterByReferenceOpenGenericMethod =
        typeof(PropertyHelper).GetMethod(nameof(CallNullSafePropertyGetterByReference), DeclaredOnlyLookup)!;

    private static readonly MethodInfo CallPropertySetterOpenGenericMethod =
        typeof(PropertyHelper).GetMethod(nameof(CallPropertySetter), DeclaredOnlyLookup)!;

    // Using an array rather than IEnumerable, as target will be called on the hot path numerous times.
    private static readonly ConcurrentDictionary<Type, PropertyHelper[]> PropertiesCache = new();

    private static readonly ConcurrentDictionary<Type, PropertyHelper[]> VisiblePropertiesCache = new();

    private Action<object, object?>? _valueSetter;
    private Func<object, object?>? _valueGetter;

    /// <summary>
    /// Initializes a fast <see cref="PropertyHelper"/>.
    /// This constructor does not cache the helper. For caching, use <see cref="GetProperties(Type)"/>.
    /// </summary>
    public PropertyHelper(PropertyInfo property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Name = property.Name;
    }

    /// <summary>
    /// Gets the backing <see cref="PropertyInfo"/>.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// Gets (or sets in derived types) the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the property value getter.
    /// </summary>
    public Func<object, object?> ValueGetter
    {
        [RequiresUnreferencedCode("This API is not trim safe.")]
        get
        {
            return _valueGetter ??= MakeFastPropertyGetter(Property);
        }
    }

    /// <summary>
    /// Gets the property value setter.
    /// </summary>
    public Action<object, object?> ValueSetter
    {
        [RequiresUnreferencedCode("This API is not trim safe.")]
        get
        {
            return _valueSetter ??= MakeFastPropertySetter(Property);
        }
    }

    /// <summary>
    /// Returns the property value for the specified <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">The object whose property value will be returned.</param>
    /// <returns>The property value.</returns>
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public object? GetValue(object instance)
    {
        return ValueGetter(instance);
    }

    /// <summary>
    /// Sets the property value for the specified <paramref name="instance" />.
    /// </summary>
    /// <param name="instance">The object whose property value will be set.</param>
    /// <param name="value">The property value.</param>
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public void SetValue(object instance, object? value)
    {
        ValueSetter(instance, value);
    }

    /// <summary>
    /// Creates and caches fast property helpers that expose getters for every public get property on the
    /// specified type.
    /// </summary>
    /// <param name="type">The type to extract property accessors for.</param>
    /// <returns>A cached array of all public properties of the specified type.
    /// </returns>
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public static PropertyHelper[] GetProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
    {
        return GetProperties(type, PropertiesCache);
    }

    /// <summary>
    /// <para>
    /// Creates and caches fast property helpers that expose getters for every non-hidden get property
    /// on the specified type.
    /// </para>
    /// <para>
    /// <see cref="M:GetVisibleProperties"/> excludes properties defined on base types that have been
    /// hidden by definitions using the <c>new</c> keyword.
    /// </para>
    /// </summary>
    /// <param name="type">The type to extract property accessors for.</param>
    /// <returns>
    /// A cached array of all public properties of the specified type.
    /// </returns>
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public static PropertyHelper[] GetVisibleProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
    {
        return GetVisibleProperties(type, PropertiesCache, VisiblePropertiesCache);
    }

    /// <summary>
    /// Creates a single fast property getter. The result is not cached.
    /// </summary>
    /// <param name="propertyInfo">propertyInfo to extract the getter for.</param>
    /// <returns>a fast getter.</returns>
    /// <remarks>
    /// This method is more memory efficient than a dynamically compiled lambda, and about the
    /// same speed.
    /// </remarks>
    [RequiresUnreferencedCode("This API is not trimmer safe.")]
    public static Func<object, object?> MakeFastPropertyGetter(PropertyInfo propertyInfo)
    {
        Debug.Assert(propertyInfo != null);

        return MakeFastPropertyGetter(
            propertyInfo,
            CallPropertyGetterOpenGenericMethod,
            CallPropertyGetterByReferenceOpenGenericMethod);
    }

    /// <summary>
    /// Creates a single fast property getter which is safe for a null input object. The result is not cached.
    /// </summary>
    /// <param name="propertyInfo">propertyInfo to extract the getter for.</param>
    /// <returns>a fast getter.</returns>
    /// <remarks>
    /// This method is more memory efficient than a dynamically compiled lambda, and about the
    /// same speed.
    /// </remarks>
    [RequiresUnreferencedCode("This API is not trimmer safe.")]
    public static Func<object, object?> MakeNullSafeFastPropertyGetter(PropertyInfo propertyInfo)
    {
        Debug.Assert(propertyInfo != null);

        return MakeFastPropertyGetter(
            propertyInfo,
            CallNullSafePropertyGetterOpenGenericMethod,
            CallNullSafePropertyGetterByReferenceOpenGenericMethod);
    }

    [RequiresUnreferencedCode("This API is not trimmer safe.")]
    private static Func<object, object?> MakeFastPropertyGetter(
        PropertyInfo propertyInfo,
        MethodInfo propertyGetterWrapperMethod,
        MethodInfo propertyGetterByRefWrapperMethod)
    {
        Debug.Assert(propertyInfo != null);

        // Must be a generic method with a Func<,> parameter
        Debug.Assert(propertyGetterWrapperMethod != null);
        Debug.Assert(propertyGetterWrapperMethod.IsGenericMethodDefinition);
        Debug.Assert(propertyGetterWrapperMethod.GetParameters().Length == 2);

        // Must be a generic method with a ByRefFunc<,> parameter
        Debug.Assert(propertyGetterByRefWrapperMethod != null);
        Debug.Assert(propertyGetterByRefWrapperMethod.IsGenericMethodDefinition);
        Debug.Assert(propertyGetterByRefWrapperMethod.GetParameters().Length == 2);

        var getMethod = propertyInfo.GetMethod;
        Debug.Assert(getMethod != null);
        Debug.Assert(!getMethod.IsStatic);
        Debug.Assert(getMethod.GetParameters().Length == 0);

        // MakeGenericMethod + value type requires IsDynamicCodeSupported to be true.
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            // Instance methods in the CLR can be turned into static methods where the first parameter
            // is open over "target". This parameter is always passed by reference, so we have a code
            // path for value types and a code path for reference types.
            if (getMethod.DeclaringType!.IsValueType)
            {
                // Create a delegate (ref TDeclaringType) -> TValue
                return MakeFastPropertyGetter(
                    typeof(ByRefFunc<,>),
                    getMethod,
                    propertyGetterByRefWrapperMethod);
            }
            else
            {
                // Create a delegate TDeclaringType -> TValue
                return MakeFastPropertyGetter(
                    typeof(Func<,>),
                    getMethod,
                    propertyGetterWrapperMethod);
            }
        }
        else
        {
            return propertyInfo.GetValue;
        }
    }

    [RequiresUnreferencedCode("This API is not trimmer safe.")]
    [RequiresDynamicCode("This API requires dynamic code because it makes generic types which may be filled with ValueTypes.")]
    private static Func<object, object?> MakeFastPropertyGetter(
        Type openGenericDelegateType,
        MethodInfo propertyGetMethod,
        MethodInfo openGenericWrapperMethod)
    {
        var typeInput = propertyGetMethod.DeclaringType!;
        var typeOutput = propertyGetMethod.ReturnType;

        var delegateType = openGenericDelegateType.MakeGenericType(typeInput, typeOutput);
        var propertyGetterDelegate = propertyGetMethod.CreateDelegate(delegateType);

        var wrapperDelegateMethod = openGenericWrapperMethod.MakeGenericMethod(typeInput, typeOutput);
        var accessorDelegate = wrapperDelegateMethod.CreateDelegate(
            typeof(Func<object, object?>),
            propertyGetterDelegate);

        return (Func<object, object?>)accessorDelegate;
    }

    /// <summary>
    /// Creates a single fast property setter for reference types. The result is not cached.
    /// </summary>
    /// <param name="propertyInfo">propertyInfo to extract the setter for.</param>
    /// <returns>a fast getter.</returns>
    /// <remarks>
    /// This method is more memory efficient than a dynamically compiled lambda, and about the
    /// same speed. This only works for reference types.
    /// </remarks>
    [RequiresUnreferencedCode("This API is not trimmer safe.")]
    public static Action<object, object?> MakeFastPropertySetter(PropertyInfo propertyInfo)
    {
        Debug.Assert(propertyInfo != null);
        Debug.Assert(!propertyInfo.DeclaringType!.IsValueType);

        var setMethod = propertyInfo.SetMethod;
        Debug.Assert(setMethod != null);
        Debug.Assert(!setMethod.IsStatic);
        Debug.Assert(setMethod.ReturnType == typeof(void));
        var parameters = setMethod.GetParameters();
        Debug.Assert(parameters.Length == 1);

        // MakeGenericMethod + value type requires IsDynamicCodeSupported to be true.
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            // Instance methods in the CLR can be turned into static methods where the first parameter
            // is open over "target". This parameter is always passed by reference, so we have a code
            // path for value types and a code path for reference types.
            var typeInput = setMethod.DeclaringType!;
            var parameterType = parameters[0].ParameterType;

            // Create a delegate TDeclaringType -> { TDeclaringType.Property = TValue; }
            var propertySetterAsAction =
                setMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeInput, parameterType));
            var callPropertySetterClosedGenericMethod =
                CallPropertySetterOpenGenericMethod.MakeGenericMethod(typeInput, parameterType);
            var callPropertySetterDelegate =
                callPropertySetterClosedGenericMethod.CreateDelegate(
                    typeof(Action<object, object?>), propertySetterAsAction);

            return (Action<object, object?>)callPropertySetterDelegate;
        }
        else
        {
            return propertyInfo.SetValue;
        }
    }

    /// <summary>
    /// Given an object, adds each instance property with a public get method as a key and its
    /// associated value to a dictionary.
    ///
    /// If the object is already an <see cref="IDictionary{String, Object}"/> instance, then a copy
    /// is returned.
    /// </summary>
    /// <remarks>
    /// The implementation of PropertyHelper will cache the property accessors per-type. This is
    /// faster when the same type is used multiple times with ObjectToDictionary.
    /// </remarks>
    [RequiresUnreferencedCode("Method uses reflection to generate the dictionary.")]
    public static IDictionary<string, object?> ObjectToDictionary(object? value)
    {
        if (value is IDictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (value is not null)
        {
            foreach (var helper in GetProperties(value.GetType(), PropertiesCache))
            {
                dictionary[helper.Name] = helper.GetValue(value);
            }
        }

        return dictionary;
    }

    // Called via reflection
    private static object? CallPropertyGetter<TDeclaringType, TValue>(
        Func<TDeclaringType, TValue> getter,
        object target)
    {
        return getter((TDeclaringType)target);
    }

    // Called via reflection
    private static object? CallPropertyGetterByReference<TDeclaringType, TValue>(
        ByRefFunc<TDeclaringType, TValue> getter,
        object target)
    {
        var unboxed = (TDeclaringType)target;
        return getter(ref unboxed);
    }

    // Called via reflection
    private static object? CallNullSafePropertyGetter<TDeclaringType, TValue>(
        Func<TDeclaringType, TValue> getter,
        object target)
    {
        if (target == null)
        {
            return null;
        }

        return getter((TDeclaringType)target);
    }

    // Called via reflection
    private static object? CallNullSafePropertyGetterByReference<TDeclaringType, TValue>(
        ByRefFunc<TDeclaringType, TValue> getter,
        object target)
    {
        if (target == null)
        {
            return null;
        }

        var unboxed = (TDeclaringType)target;
        return getter(ref unboxed);
    }

    private static void CallPropertySetter<TDeclaringType, TValue>(
        Action<TDeclaringType, TValue> setter,
        object target,
        object value)
    {
        setter((TDeclaringType)target, (TValue)value);
    }

    /// <summary>
    /// <para>
    /// Creates and caches fast property helpers that expose getters for every non-hidden get property
    /// on the specified type.
    /// </para>
    /// <para>
    /// <see cref="M:GetVisibleProperties"/> excludes properties defined on base types that have been
    /// hidden by definitions using the <c>new</c> keyword.
    /// </para>
    /// </summary>
    /// <param name="type">The type to extract property accessors for.</param>
    /// <param name="allPropertiesCache">The cache to store results in. Use <see cref="PropertiesCache"/> to use the default cache. Use <see langword="null"/> to avoid caching.</param>
    /// <param name="visiblePropertiesCache">The cache to store results in. Use <see cref="VisiblePropertiesCache"/> if the calling type does not have its own independent cache. Use <see langword="null"/> to avoid caching.</param>
    /// <returns>
    /// A cached array of all public properties of the specified type.
    /// </returns>
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public static PropertyHelper[] GetVisibleProperties(
        Type type,
        ConcurrentDictionary<Type, PropertyHelper[]>? allPropertiesCache,
        ConcurrentDictionary<Type, PropertyHelper[]>? visiblePropertiesCache)
    {
        if (visiblePropertiesCache is not null && visiblePropertiesCache.TryGetValue(type, out var result))
        {
            return result;
        }

        // The simple and common case, this is normal POCO object - no need to allocate.
        var allPropertiesDefinedOnType = true;
        var allProperties = GetProperties(type, allPropertiesCache);
        foreach (var propertyHelper in allProperties)
        {
            if (propertyHelper.Property.DeclaringType != type)
            {
                allPropertiesDefinedOnType = false;
                break;
            }
        }

        if (allPropertiesDefinedOnType)
        {
            result = allProperties;
            visiblePropertiesCache?.TryAdd(type, result);
            return result;
        }

        // There's some inherited properties here, so we need to check for hiding via 'new'.
        var filteredProperties = new List<PropertyHelper>(allProperties.Length);
        foreach (var propertyHelper in allProperties)
        {
            var declaringType = propertyHelper.Property.DeclaringType;
            if (declaringType == type)
            {
                filteredProperties.Add(propertyHelper);
                continue;
            }

            // If this property was declared on a base type then look for the definition closest to the
            // the type to see if we should include it.
            var ignoreProperty = false;

            // Walk up the hierarchy until we find the type that actually declares this
            // PropertyInfo.
            Type? currentType = type;
            while (currentType != null && currentType != declaringType)
            {
                // We've found a 'more proximal' public definition
                var declaredProperty = currentType.GetProperty(propertyHelper.Name, DeclaredOnlyLookup);
                if (declaredProperty != null)
                {
                    ignoreProperty = true;
                    break;
                }

                currentType = currentType.BaseType;
            }

            if (!ignoreProperty)
            {
                filteredProperties.Add(propertyHelper);
            }
        }

        result = filteredProperties.ToArray();
        visiblePropertiesCache?.TryAdd(type, result);
        return result;
    }

    /// <summary>
    /// Creates and caches fast property helpers that expose getters for every public get property on the
    /// specified type.
    /// </summary>
    /// <param name="type">The type to extract property accessors for.</param>
    /// <param name="cache">The cache to store results in. Use <see cref="PropertiesCache"/> to use the default cache. Use <see langword="null"/> to avoid caching.</param>
    /// <returns>A cached array of all public properties of the specified type.
    /// </returns>
    // There isn't a way to represent trimmability requirements since for type since we unwrap nullable types.
    [RequiresUnreferencedCode("This API is not trim safe.")]
    public static PropertyHelper[] GetProperties(
        Type type,
        ConcurrentDictionary<Type, PropertyHelper[]>? cache)
    {
        // Unwrap nullable types. This means Nullable<T>.Value and Nullable<T>.HasValue will not be
        // part of the sequence of properties returned by this method.
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (cache is null || !cache.TryGetValue(type, out var result))
        {
            var propertyHelpers = new List<PropertyHelper>();
            // We avoid loading indexed properties using the Where statement.
            AddInterestingProperties(propertyHelpers, type);

            if (type.IsInterface)
            {
                // Reflection does not return information about inherited properties on the interface itself.
                foreach (var @interface in type.GetInterfaces())
                {
                    AddInterestingProperties(propertyHelpers, @interface);
                }
            }

            result = propertyHelpers.ToArray();
            cache?.TryAdd(type, result);
        }

        return result;

        static void AddInterestingProperties(
            List<PropertyHelper> propertyHelpers,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
        {
            foreach (var property in type.GetProperties(Everything))
            {
                if (!IsInterestingProperty(property))
                {
                    continue;
                }

                propertyHelpers.Add(new PropertyHelper(property));
            }
        }
    }

    private static bool IsInterestingProperty(PropertyInfo property)
    {
        // For improving application startup time, do not use GetIndexParameters() api early in this check as it
        // creates a copy of parameter array and also we would like to check for the presence of a get method
        // and short circuit asap.
        return
            property.GetMethod != null &&
            property.GetMethod.IsPublic &&
            !property.GetMethod.IsStatic &&

            // PropertyHelper can't really interact with ref-struct properties since they can't be
            // boxed and can't be used as generic types. We just ignore them.
            //
            // see: https://github.com/aspnet/Mvc/issues/8545
            !property.PropertyType.IsByRefLike &&

            // Indexed properties are not useful (or valid) for grabbing properties off an object.
            property.GetMethod.GetParameters().Length == 0;
    }

    internal static class MetadataUpdateHandler
    {
        /// <summary>
        /// Invoked as part of <see cref="MetadataUpdateHandlerAttribute" /> contract for hot reload.
        /// </summary>
        public static void ClearCache(Type[]? _)
        {
            PropertiesCache.Clear();
            VisiblePropertiesCache.Clear();
        }
    }
}
