// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

internal static class ParameterPolicyActivator
{
    public static T? ResolveParameterPolicy<T>(
        IDictionary<string, Type> inlineParameterPolicyMap,
        IServiceProvider? serviceProvider,
        string inlineParameterPolicy,
        out string? parameterPolicyKey)
        where T : IParameterPolicy
    {
        // IServiceProvider could be null
        // DefaultInlineConstraintResolver can be created without an IServiceProvider and then call this method

        if (!ResolveParameterPolicyTypeAndArgument(
            inlineParameterPolicyMap,
            inlineParameterPolicy,
            out parameterPolicyKey,
            out var argumentString,
            out var parameterPolicyType))
        {
            return default;
        }

        if (!typeof(T).IsAssignableFrom(parameterPolicyType))
        {
            if (!typeof(IParameterPolicy).IsAssignableFrom(parameterPolicyType))
            {
                // Error if type is not a parameter policy
                throw new RouteCreationException(
                            Resources.FormatDefaultInlineConstraintResolver_TypeNotConstraint(
                                                        parameterPolicyType, parameterPolicyKey, typeof(T).Name));
            }

            // Return null if type is parameter policy but is not the exact type
            // This is used by IInlineConstraintResolver for backwards compatibility
            // e.g. looking for an IRouteConstraint but get a different IParameterPolicy type
            return default;
        }

        try
        {
            return (T)CreateParameterPolicy(serviceProvider, parameterPolicyType, argumentString);
        }
        catch (RouteCreationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new RouteCreationException(
                $"An error occurred while trying to create an instance of '{parameterPolicyType.FullName}'.",
                exception);
        }
    }

    private static bool ResolveParameterPolicyTypeAndArgument(
        IDictionary<string, Type> inlineParameterPolicyMap,
        string inlineParameterPolicy,
        out string? policyKey,
        out string? argumentString,
        [NotNullWhen(true)] out Type? policyType)
    {
        ArgumentNullException.ThrowIfNull(inlineParameterPolicy);

        var indexOfFirstOpenParens = inlineParameterPolicy.IndexOf('(');
        if (indexOfFirstOpenParens >= 0 && inlineParameterPolicy.EndsWith(')'))
        {
            policyKey = inlineParameterPolicy.Substring(0, indexOfFirstOpenParens);
            argumentString = inlineParameterPolicy.Substring(
                indexOfFirstOpenParens + 1,
                inlineParameterPolicy.Length - indexOfFirstOpenParens - 2);
        }
        else
        {
            policyKey = inlineParameterPolicy;
            argumentString = null;
        }

        return inlineParameterPolicyMap.TryGetValue(policyKey, out policyType);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern", Justification = "This type comes from the ConstraintMap.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "We ensure the constructor is preserved when the constraint map is added.")]
    private static IParameterPolicy CreateParameterPolicy(IServiceProvider? serviceProvider, Type parameterPolicyType, string? argumentString)
    {
        ConstructorInfo? activationConstructor = null;
        object?[]? parameters = null;
        var constructors = parameterPolicyType.GetConstructors();

        // If there is only one constructor and it has a single parameter, pass the argument string directly
        // This is necessary for the Regex RouteConstraint to ensure that patterns are not split on commas.
        if (constructors.Length == 1 && GetNonConvertableParameterTypeCount(serviceProvider, constructors[0].GetParameters()) == 1)
        {
            activationConstructor = constructors[0];
            parameters = ConvertArguments(serviceProvider, activationConstructor.GetParameters(), new string?[] { argumentString });
        }
        else
        {
            var arguments = argumentString?.Split(',', StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

            // We want to find the constructors that match the number of passed in arguments
            // We either want a single match, or a single best match. The best match is the one with the most
            // arguments that can be resolved from DI
            //
            // For example, ctor(string, IService) will beat ctor(string)
            var matchingConstructors = constructors
                .Where(ci => GetNonConvertableParameterTypeCount(serviceProvider, ci.GetParameters()) == arguments.Length)
                .OrderByDescending(ci => ci.GetParameters().Length)
                .ToArray();

            if (matchingConstructors.Length == 0)
            {
                throw new RouteCreationException(
                            Resources.FormatDefaultInlineConstraintResolver_CouldNotFindCtor(
                                                   parameterPolicyType.Name, arguments.Length));
            }
            else
            {
                // When there are multiple matching constructors, choose the one with the most service arguments
                if (matchingConstructors.Length == 1
                    || matchingConstructors[0].GetParameters().Length > matchingConstructors[1].GetParameters().Length)
                {
                    activationConstructor = matchingConstructors[0];
                }
                else
                {
                    throw new RouteCreationException(
                                Resources.FormatDefaultInlineConstraintResolver_AmbiguousCtors(
                                                       parameterPolicyType.Name, matchingConstructors[0].GetParameters().Length));
                }

                parameters = ConvertArguments(serviceProvider, activationConstructor.GetParameters(), arguments);
            }
        }

        return (IParameterPolicy)activationConstructor.Invoke(parameters);
    }

    private static int GetNonConvertableParameterTypeCount(IServiceProvider? serviceProvider, ParameterInfo[] parameters)
    {
        if (serviceProvider == null)
        {
            return parameters.Length;
        }

        var count = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (typeof(IConvertible).IsAssignableFrom(parameters[i].ParameterType))
            {
                count++;
            }
        }

        return count;
    }

    private static object?[] ConvertArguments(IServiceProvider? serviceProvider, ParameterInfo[] parameterInfos, string?[] arguments)
    {
        var parameters = new object?[parameterInfos.Length];
        var argumentPosition = 0;
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var parameter = parameterInfos[i];
            var parameterType = parameter.ParameterType;

            if (serviceProvider != null && !typeof(IConvertible).IsAssignableFrom(parameterType))
            {
                parameters[i] = serviceProvider.GetRequiredService(parameterType);
            }
            else
            {
                parameters[i] = Convert.ChangeType(arguments[argumentPosition], parameterType, CultureInfo.InvariantCulture);
                argumentPosition++;
            }
        }

        return parameters;
    }
}
