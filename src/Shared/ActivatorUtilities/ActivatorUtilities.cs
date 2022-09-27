// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Microsoft.Extensions.Internal;

internal static class ActivatorUtilities
{
    private const DynamicallyAccessedMemberTypes ActivatorAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors;

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="instanceType">The type to activate</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type instanceType</returns>
    public static object CreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(ActivatorAccessibility)] Type instanceType,
        params object[] parameters)
    {
        var bestLength = -1;

        ConstructorMatcher bestMatcher = default;

        if (!instanceType.IsAbstract)
        {
            foreach (var constructor in instanceType.GetConstructors())
            {
                var matcher = new ConstructorMatcher(constructor);
                var length = matcher.Match(parameters);

                if (bestLength < length)
                {
                    bestLength = length;
                    bestMatcher = matcher;
                }
            }
        }

        if (bestLength == -1)
        {
            var message = $"A suitable constructor for type '{instanceType}' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            throw new InvalidOperationException(message);
        }

        return bestMatcher.CreateInstance(provider);
    }

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type to activate</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type T</returns>
    public static T CreateInstance<
        [DynamicallyAccessedMembers(ActivatorAccessibility)] T
        >(IServiceProvider provider, params object[] parameters)
    {
        return (T)CreateInstance(provider, typeof(T), parameters);
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <typeparam name="T">The type of the service</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <returns>The resolved service or created instance</returns>
    public static T GetServiceOrCreateInstance<
        [DynamicallyAccessedMembers(ActivatorAccessibility)] T
        >(IServiceProvider provider)
    {
        return (T)GetServiceOrCreateInstance(provider, typeof(T));
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <param name="provider">The service provider</param>
    /// <param name="type">The type of the service</param>
    /// <returns>The resolved service or created instance</returns>
    public static object GetServiceOrCreateInstance(
        IServiceProvider provider,
        [DynamicallyAccessedMembers(ActivatorAccessibility)] Type type)
    {
        return provider.GetService(type) ?? CreateInstance(provider, type);
    }

    private struct ConstructorMatcher
    {
        private readonly ConstructorInfo _constructor;
        private readonly ParameterInfo[] _parameters;
        private readonly object?[] _parameterValues;

        public ConstructorMatcher(ConstructorInfo constructor)
        {
            _constructor = constructor;
            _parameters = _constructor.GetParameters();
            _parameterValues = new object?[_parameters.Length];
        }

        public int Match(object[] givenParameters)
        {
            var applyIndexStart = 0;
            var applyExactLength = 0;
            for (var givenIndex = 0; givenIndex != givenParameters.Length; givenIndex++)
            {
                var givenType = givenParameters[givenIndex]?.GetType();
                var givenMatched = false;

                for (var applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
                {
                    if (_parameterValues[applyIndex] == null &&
                        _parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                    {
                        givenMatched = true;
                        _parameterValues[applyIndex] = givenParameters[givenIndex];
                        if (applyIndexStart == applyIndex)
                        {
                            applyIndexStart++;
                            if (applyIndex == givenIndex)
                            {
                                applyExactLength = applyIndex;
                            }
                        }
                    }
                }

                if (givenMatched == false)
                {
                    return -1;
                }
            }
            return applyExactLength;
        }

        public object CreateInstance(IServiceProvider provider)
        {
            for (var index = 0; index < _parameters.Length; index++)
            {
                var parameter = _parameters[index];

                if (_parameterValues[index] is null)
                {
                    var value = provider.GetService(parameter.ParameterType);
                    if (value is null)
                    {
                        if (!ParameterDefaultValue.TryGetDefaultValue(parameter, out var defaultValue))
                        {
                            throw new InvalidOperationException($"Unable to resolve service for type '{_parameters[index].ParameterType}' while attempting to activate '{_constructor.DeclaringType}'.");
                        }

                        value = defaultValue;
                    }

                    _parameterValues[index] = value;
                }
            }

#if NETCOREAPP
            return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: _parameterValues, culture: null);
#else
            try
            {
                return _constructor.Invoke(_parameterValues);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // The above line will always throw, but the compiler requires we throw explicitly.
                throw;
            }
#endif
        }
    }
}

