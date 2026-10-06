// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.Tests;

public class JSInteropTrimmingAnnotationsTest
{
    [Theory]
    [InlineData(typeof(IJSRuntime), 8)]
    [InlineData(typeof(IJSObjectReference), 8)]
    [InlineData(typeof(JSRuntime), 8)]
    [InlineData(typeof(JSObjectReference), 8)]
    [InlineData(typeof(IJSInProcessRuntime), 4)]
    [InlineData(typeof(IJSInProcessObjectReference), 4)]
    [InlineData(typeof(JSInProcessRuntime), 4)]
    [InlineData(typeof(JSInProcessObjectReference), 4)]
    [InlineData(typeof(JSRuntimeExtensions), 11)]
    [InlineData(typeof(JSObjectReferenceExtensions), 11)]
    [InlineData(typeof(JSInProcessRuntimeExtensions), 1)]
    [InlineData(typeof(JSInProcessObjectReferenceExtensions), 1)]
    public void SerializationMethods_RequireUnreferencedCode(Type type, int expectedMethodCount)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name.StartsWith("Invoke", StringComparison.Ordinal)
                || method.Name.StartsWith("GetValue", StringComparison.Ordinal)
                || method.Name.StartsWith("SetValue", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(expectedMethodCount, methods.Length);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();
            Assert.True(attribute is not null, $"{type.Name}.{method} must warn about trimming JSON-serialized types.");
            Assert.Equal("JSON serialization and deserialization might require types that cannot be statically analyzed.", attribute.Message);

            foreach (var parameter in method.GetGenericArguments())
            {
                var members = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
                Assert.NotNull(members);
                Assert.Equal(Microsoft.AspNetCore.Internal.LinkerFlags.JsonSerialized, members.MemberTypes);
            }
        }
    }
}
