// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// MSTest assertion class to provide convenience and assert methods for generic types
    /// whose type parameters are not known at compile time.
    /// </summary>
    public class GenericTypeAssert
    {
        private static readonly GenericTypeAssert singleton = new GenericTypeAssert();

        public static GenericTypeAssert Singleton { get { return singleton; } }

        /// <summary>
        /// Asserts the given <paramref name="genericBaseType"/> is a generic type and creates a new
        /// bound generic type using <paramref name="genericParameterType"/>.  It then asserts there
        /// is a constructor that will accept <paramref name="parameterTypes"/> and returns it.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <returns>The <see cref="ConstructorInfo"/> of that constructor which may be invoked to create that new generic type.</returns>
        public ConstructorInfo GetConstructor(Type genericBaseType, Type genericParameterType, params Type[] parameterTypes)
        {
            Assert.NotNull(genericBaseType);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterType);
            Assert.NotNull(parameterTypes);

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });
            ConstructorInfo ctor = genericType.GetConstructor(parameterTypes);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return ctor;
        }

        /// <summary>
        /// Asserts the given <paramref name="genericBaseType"/> is a generic type and creates a new
        /// bound generic type using <paramref name="genericParameterType"/>.  It then asserts there
        /// is a constructor that will accept <paramref name="parameterTypes"/> and returns it.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <returns>The <see cref="ConstructorInfo"/> of that constructor which may be invoked to create that new generic type.</returns>
        public ConstructorInfo GetConstructor(Type genericBaseType, Type[] genericParameterTypes, params Type[] parameterTypes)
        {
            Assert.NotNull(genericBaseType);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterTypes);
            Assert.NotNull(parameterTypes);

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);
            ConstructorInfo ctor = genericType.GetConstructor(parameterTypes);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return ctor;
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public object InvokeConstructor(Type genericBaseType, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterType, parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypse">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public object InvokeConstructor(Type genericBaseType, Type[] genericParameterTypes, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterTypes, parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from the types of <paramref name="parameterValues"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public object InvokeConstructor(Type genericBaseType, Type genericParameterType, params object[] parameterValues)
        {
            Assert.NotNull(genericBaseType);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterType);

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from the types of <paramref name="parameterValues"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public object InvokeConstructor(Type genericBaseType, Type[] genericParameterTypes, params object[] parameterValues)
        {
            Assert.NotNull(genericBaseType);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterTypes);

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public T InvokeConstructor<T>(Type genericBaseType, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterType, parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public T InvokeConstructor<T>(Type genericBaseType, Type[] genericParameterTypes, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterTypes, parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public T InvokeConstructor<T>(Type genericBaseType, Type genericParameterType, params object[] parameterValues)
        {
            Assert.NotNull(genericBaseType == null);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterType);

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public T InvokeConstructor<T>(Type genericBaseType, Type[] genericParameterTypes, params object[] parameterValues)
        {
            Assert.NotNull(genericBaseType);
            Assert.True(genericBaseType.IsGenericTypeDefinition);
            Assert.NotNull(genericParameterTypes);

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.True(ctor != null, String.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Asserts the given instance is one from a generic type of the specified parameter type.
        /// </summary>
        /// <typeparam name="T">The type of instance.</typeparam>
        /// <param name="instance">The instance to test.</param>
        /// <param name="genericTypeParameter">The type of the generic parameter to which the instance's generic type should have been bound.</param>
        public void IsCorrectGenericType<T>(T instance, Type genericTypeParameter)
        {
            Assert.NotNull(instance);
            Assert.NotNull(genericTypeParameter);
            Assert.True(instance.GetType().IsGenericType);
            Type[] genericArguments = instance.GetType().GetGenericArguments();
            Assert.Equal(1, genericArguments.Length);
            Assert.Equal(genericTypeParameter, genericArguments[0]);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeMethod(object instance, string methodName, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.NotNull(instance);
            Assert.NotNull(parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            MethodInfo methodInfo = instance.GetType().GetMethod(methodName, parameterTypes);
            Assert.NotNull(methodInfo);
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeMethod(Type type, string methodName, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.NotNull(type);
            Assert.NotNull(parameterTypes);
            Assert.NotNull(parameterValues);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes);
            Assert.NotNull(methodInfo);
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public MethodInfo CreateGenericMethod(Type type, string methodName, Type genericParameterType, Type[] parameterTypes)
        {
            Assert.NotNull(type);
            Assert.NotNull(parameterTypes);
            Assert.NotNull(genericParameterType);
            //MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes);
            MethodInfo methodInfo = type.GetMethods().Where((m) => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) && m.IsGenericMethod && AreAssignableFrom(m.GetParameters(), parameterTypes)).FirstOrDefault();
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsGenericMethod);
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            Assert.NotNull(genericMethod);
            return genericMethod;
        }

        /// <summary>
        /// Invokes via Reflection the static generic method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeGenericMethod(Type type, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            MethodInfo methodInfo = CreateGenericMethod(type, methodName, genericParameterType, parameterTypes);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the generic method on the given instance.
        /// </summary>
        /// <param name="instance">The instance on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeGenericMethod(object instance, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.NotNull(instance);
            MethodInfo methodInfo = CreateGenericMethod(instance.GetType(), methodName, genericParameterType, parameterTypes);
            Assert.Equal(parameterTypes.Length, parameterValues.Length);
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the generic method on the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the method.</typeparam>
        /// <param name="instance">The instance on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public T InvokeGenericMethod<T>(object instance, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            return (T)InvokeGenericMethod(instance, methodName, genericParameterType, parameterTypes, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeMethod(object instance, string methodName, params object[] parameterValues)
        {
            Assert.NotNull(instance);
            MethodInfo methodInfo = FindMethod(instance.GetType(), methodName, parameterValues);
            Assert.NotNull(methodInfo);
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeMethod(Type type, string methodName, params object[] parameterValues)
        {
            Assert.NotNull(type);
            MethodInfo methodInfo = FindMethod(type, methodName, parameterValues);
            Assert.NotNull(methodInfo);
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The type of the generic parameter.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeGenericMethod(object instance, string methodName, Type genericParameterType, params object[] parameterValues)
        {
            Assert.NotNull(instance);
            Assert.NotNull(genericParameterType);
            MethodInfo methodInfo = FindMethod(instance.GetType(), methodName, parameterValues);
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsGenericMethod);
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            return genericMethod.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The type of the generic parameter.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public object InvokeGenericMethod(Type type, string methodName, Type genericParameterType, params object[] parameterValues)
        {
            Assert.NotNull(type);
            Assert.NotNull(genericParameterType);
            MethodInfo methodInfo = FindMethod(type, methodName, parameterValues);
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsGenericMethod);
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            return genericMethod.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Retrieves the value from the specified property.
        /// </summary>
        /// <param name="instance">The instance containing the property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="failureMessage">The error message to prefix any test assertions.</param>
        /// <returns>The value returned from the property.</returns>
        public object GetProperty(object instance, string propertyName, string failureMessage)
        {
            PropertyInfo propertyInfo = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(propertyInfo);
            return propertyInfo.GetValue(instance, null);
        }

        private static bool AreAssignableFrom(Type[] parameterTypes, params object[] parameterValues)
        {
            Assert.NotNull(parameterTypes);
            Assert.NotNull(parameterValues);
            if (parameterTypes.Length != parameterValues.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                if (!parameterTypes[i].IsInstanceOfType(parameterValues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAssignableFrom(ParameterInfo[] parameterInfos, params Type[] parameterTypes)
        {
            Assert.NotNull(parameterInfos);
            Assert.NotNull(parameterTypes);
            Type[] parameterInfoTypes = parameterInfos.Select<ParameterInfo, Type>((info) => info.ParameterType).ToArray();
            if (parameterInfoTypes.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterInfoTypes.Length; ++i)
            {
                // Generic parameters are assumed to be assignable
                if (parameterInfoTypes[i].IsGenericParameter)
                {
                    continue;
                }

                if (!parameterInfoTypes[i].IsAssignableFrom(parameterTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAssignableFrom(ParameterInfo[] parameterInfos, params object[] parameterValues)
        {
            Assert.NotNull(parameterInfos);
            Assert.NotNull(parameterValues);
            Type[] parameterTypes = parameterInfos.Select<ParameterInfo, Type>((info) => info.ParameterType).ToArray();
            return AreAssignableFrom(parameterTypes, parameterValues);
        }

        private static ConstructorInfo FindConstructor(Type type, params object[] parameterValues)
        {
            Assert.NotNull(type);
            Assert.NotNull(parameterValues);
            return type.GetConstructors().FirstOrDefault((c) => AreAssignableFrom(c.GetParameters(), parameterValues));
        }

        private static MethodInfo FindMethod(Type type, string methodName, params object[] parameterValues)
        {
            Assert.NotNull(type);
            Assert.False(String.IsNullOrWhiteSpace(methodName));
            Assert.NotNull(parameterValues);
            return type.GetMethods().FirstOrDefault((m) => String.Equals(m.Name, methodName, StringComparison.Ordinal) && AreAssignableFrom(m.GetParameters(), parameterValues));
        }
    }
}
