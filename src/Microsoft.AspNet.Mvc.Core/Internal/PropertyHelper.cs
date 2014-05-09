// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    internal class PropertyHelper
    {
        // Delegate type for a by-ref property getter
        private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

        private static readonly MethodInfo CallPropertyGetterOpenGenericMethod = 
            typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod("CallPropertyGetter");

        private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod =
            typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod("CallPropertyGetterByReference");

        private static readonly ConcurrentDictionary<Type, PropertyHelper[]> ReflectionCache = 
            new ConcurrentDictionary<Type, PropertyHelper[]>();

        private readonly Func<object, object> _valueGetter;

        /// <summary>
        /// Initializes a fast property helper. 
        /// 
        /// This constructor does not cache the helper. For caching, use GetProperties.
        /// </summary>
        public PropertyHelper(PropertyInfo property)
        {
            Contract.Assert(property != null);

            Name = property.Name;
            _valueGetter = MakeFastPropertyGetter(property);
        }

        public virtual string Name { get; protected set; }

        public object GetValue(object instance)
        {
            return _valueGetter(instance);
        }

        /// <summary>
        /// Creates and caches fast property helpers that expose getters for every public get property on the 
        /// underlying type.
        /// </summary>
        /// <param name="instance">the instance to extract property accessors for.</param>
        /// <returns>a cached array of all public property getters from the underlying type of target instance.</returns>
        public static PropertyHelper[] GetProperties(object instance)
        {
            return GetProperties(instance, CreateInstance, ReflectionCache);
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
        public static Func<object, object> MakeFastPropertyGetter(PropertyInfo propertyInfo)
        {
            Contract.Assert(propertyInfo != null);

            var getMethod = propertyInfo.GetMethod;
            Contract.Assert(getMethod != null);
            Contract.Assert(!getMethod.IsStatic);
            Contract.Assert(getMethod.GetParameters().Length == 0);

            // Instance methods in the CLR can be turned into static methods where the first parameter
            // is open over "target". This parameter is always passed by reference, so we have a code
            // path for value types and a code path for reference types.
            var typeInput = getMethod.DeclaringType;
            var typeOutput = getMethod.ReturnType;

            Delegate callPropertyGetterDelegate;
            if (typeInput.IsValueType())
            {
                // Create a delegate (ref TDeclaringType) -> TValue
                var delegateType = typeof(ByRefFunc<,>).MakeGenericType(typeInput, typeOutput);
                var propertyGetterAsFunc = getMethod.CreateDelegate(delegateType);
                var callPropertyGetterClosedGenericMethod = 
                    CallPropertyGetterByReferenceOpenGenericMethod.MakeGenericMethod(typeInput, typeOutput);
                callPropertyGetterDelegate = 
                    callPropertyGetterClosedGenericMethod.CreateDelegate(typeof(Func<object, object>), propertyGetterAsFunc);
            }
            else
            {
                // Create a delegate TDeclaringType -> TValue
                var propertyGetterAsFunc = getMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeInput, typeOutput));
                var callPropertyGetterClosedGenericMethod = 
                    CallPropertyGetterOpenGenericMethod.MakeGenericMethod(typeInput, typeOutput);
                callPropertyGetterDelegate = 
                    callPropertyGetterClosedGenericMethod.CreateDelegate(typeof(Func<object, object>), propertyGetterAsFunc);
            }

            return (Func<object, object>)callPropertyGetterDelegate;
        }

        private static PropertyHelper CreateInstance(PropertyInfo property)
        {
            return new PropertyHelper(property);
        }

        // Called via reflection
        private static object CallPropertyGetter<TDeclaringType, TValue>(Func<TDeclaringType, TValue> getter, object target)
        {
            return getter((TDeclaringType)target);
        }

        // Called via reflection
        private static object CallPropertyGetterByReference<TDeclaringType, TValue>(
            ByRefFunc<TDeclaringType, TValue> getter, 
            object target)
        {
            var unboxed = (TDeclaringType)target;
            return getter(ref unboxed);
        }

        protected static PropertyHelper[] GetProperties(
            object instance,
            Func<PropertyInfo, PropertyHelper> createPropertyHelper,
            ConcurrentDictionary<Type, PropertyHelper[]> cache)
        {
            // Using an array rather than IEnumerable, as target will be called on the hot path numerous times.
            PropertyHelper[] helpers;

            var type = instance.GetType();

            if (!cache.TryGetValue(type, out helpers))
            {
                // We avoid loading indexed properties using the where statement.
                // Indexed properties are not useful (or valid) for grabbing properties off an anonymous object.
                var properties = type.GetRuntimeProperties().Where(
                    prop => prop.GetIndexParameters().Length == 0 &&
                    prop.GetMethod != null &&
                    prop.GetMethod.IsPublic &&
                    !prop.GetMethod.IsStatic);

                helpers = properties.Select(p => createPropertyHelper(p)).ToArray();
                cache.TryAdd(type, helpers);
            }

            return helpers;
        }
    }
}
