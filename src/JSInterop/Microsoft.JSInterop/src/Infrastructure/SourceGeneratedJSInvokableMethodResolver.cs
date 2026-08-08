// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class SourceGeneratedJSInvokableMethodResolver : IJSInvokableMethodResolver
{
    private readonly Dictionary<(string AssemblyName, string Identifier), JSInvokableMethodDescriptor> _staticMethods = [];
    private readonly Dictionary<(Type TargetType, string Identifier), JSInvokableMethodDescriptor> _instanceMethods = [];
    private readonly Dictionary<string, JSInvokableMethodDescriptor> _methodsByContributionKey = [];
    private readonly HashSet<Type> _coveredTypes = [];

    public SourceGeneratedJSInvokableMethodResolver(IReadOnlyList<JSInvokableMethodDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (descriptor.MethodKey is { } methodKey &&
                _methodsByContributionKey.TryGetValue(methodKey, out var previous) &&
                IsSameContribution(previous, descriptor))
            {
                continue;
            }

            if (descriptor is { Kind: JSInvokableMethodKind.OverrideBlocker, Identifier.Length: 0 })
            {
                _coveredTypes.Add(descriptor.TargetType);
                continue;
            }

            if (descriptor.IsStatic)
            {
                var key = (descriptor.AssemblyName, descriptor.Identifier);
                if (!_staticMethods.TryAdd(key, descriptor))
                {
                    throw new InvalidOperationException($"The assembly '{descriptor.AssemblyName}' contains more than one " +
                        $"[{nameof(JSInvokableAttribute)}] method with identifier '{descriptor.Identifier}'. All [{nameof(JSInvokableAttribute)}] methods within the same " +
                        "assembly must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [{nameof(JSInvokableAttribute)}] attribute.");
                }
            }
            else
            {
                var key = (descriptor.TargetType, descriptor.Identifier);
                if (!_instanceMethods.TryAdd(key, descriptor))
                {
                    throw new InvalidOperationException($"The type {descriptor.TargetType.Name} contains more than one " +
                        $"[{nameof(JSInvokableAttribute)}] method with identifier '{descriptor.Identifier}'. All [{nameof(JSInvokableAttribute)}] methods within the same " +
                        "type must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [{nameof(JSInvokableAttribute)}] attribute.");
                }
            }

            if (descriptor.MethodKey is { } contributionKey)
            {
                _methodsByContributionKey.TryAdd(contributionKey, descriptor);
            }
        }
    }

    public bool TryResolve(
        in JSInvokableMethodInfo methodInfo,
        [NotNullWhen(true)] out JSInvokableMethodDescriptor? descriptor)
    {
        if (methodInfo.IsStatic)
        {
            return _staticMethods.TryGetValue((methodInfo.AssemblyName!, methodInfo.Identifier), out descriptor);
        }

        JSInvokableMethodDescriptor? candidate = null;
        var isInheritedResolutionCovered = true;
        var isDirectType = true;
        for (var type = methodInfo.TargetType; type is not null; type = type.BaseType)
        {
            var lookupType = GetLookupType(type);
            if (_instanceMethods.TryGetValue((lookupType, methodInfo.Identifier), out descriptor))
            {
                if (!isDirectType && !isInheritedResolutionCovered)
                {
                    descriptor = null;
                    return false;
                }

                if (descriptor.Kind is JSInvokableMethodKind.OverrideBlocker)
                {
                    descriptor = candidate;
                    return candidate is not null;
                }

                if (descriptor.Kind is JSInvokableMethodKind.Override)
                {
                    if (candidate is null)
                    {
                        return true;
                    }
                }

                if (candidate is not null)
                {
                    throw new InvalidOperationException($"The type {methodInfo.TargetType!.Name} contains more than one " +
                        $"[{nameof(JSInvokableAttribute)}] method with identifier '{methodInfo.Identifier}'. All [{nameof(JSInvokableAttribute)}] methods within the same " +
                        "type must have different identifiers. You can pass a custom identifier as a parameter to " +
                        $"the [{nameof(JSInvokableAttribute)}] attribute.");
                }

                candidate = descriptor;
            }

            isInheritedResolutionCovered &= _coveredTypes.Contains(lookupType);
            isDirectType = false;
        }

        descriptor = candidate;
        return candidate is not null;
    }

    private static bool IsSameContribution(
        JSInvokableMethodDescriptor left,
        JSInvokableMethodDescriptor right)
        => string.Equals(left.AssemblyName, right.AssemblyName, StringComparison.Ordinal) &&
            left.TargetType == right.TargetType &&
            string.Equals(left.Identifier, right.Identifier, StringComparison.Ordinal) &&
            left.IsStatic == right.IsStatic &&
            left.Kind == right.Kind;

    private static Type GetLookupType(Type type)
        => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
}
