// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class PublicApiTest
{
    [Fact]
    public void PublicSurface_DoesNotExposeJavaScriptInteropTypes()
    {
        var assembly = typeof(IBrowserPlatform).Assembly;
        var exposedTypes = assembly.ExportedTypes
            .SelectMany(GetPublicSignatureTypes)
            .Where(ContainsJavaScriptInteropType);

        Assert.Empty(exposedTypes);
    }

    [Fact]
    public void LiveBrowserObjectMethods_AreAsynchronous()
    {
        Type[] liveBrowserObjectTypes =
        [
            typeof(Storage),
        ];

        var synchronousMethods = liveBrowserObjectTypes
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName)
            .Where(method => method.ReturnType != typeof(ValueTask))
            .Where(method => !method.ReturnType.IsGenericType ||
                method.ReturnType.GetGenericTypeDefinition() != typeof(ValueTask<>));

        Assert.Empty(synchronousMethods);
    }

    private static IEnumerable<Type> GetPublicSignatureTypes(Type type)
    {
        yield return type;

        foreach (var constructor in type.GetConstructors())
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var method in type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var property in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly))
        {
            yield return property.PropertyType;
        }
    }

    private static bool ContainsJavaScriptInteropType(Type type)
    {
        if (type == typeof(IJSRuntime) || type == typeof(IJSObjectReference))
        {
            return true;
        }

        if (type.HasElementType)
        {
            return ContainsJavaScriptInteropType(type.GetElementType()!);
        }

        return type.IsGenericType &&
            type.GetGenericArguments().Any(ContainsJavaScriptInteropType);
    }
}
