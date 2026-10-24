// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class UnsupportedJavaScriptRuntimeTest
{
    [Fact]
    public void InterfaceMethods_HaveMatchingTrimmingAnnotations()
    {
        var interfaceMap = typeof(UnsupportedJavaScriptRuntime).GetInterfaceMap(typeof(IJSRuntime));

        Assert.Equal(8, interfaceMap.InterfaceMethods.Length);
        for (var i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
        {
            var interfaceMethod = interfaceMap.InterfaceMethods[i];
            var targetMethod = interfaceMap.TargetMethods[i];
            var interfaceAttribute = interfaceMethod.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();
            var targetAttribute = targetMethod.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();

            Assert.NotNull(interfaceAttribute);
            Assert.NotNull(targetAttribute);
            Assert.Equal(interfaceAttribute.Message, targetAttribute.Message);

            var interfaceGenericArguments = interfaceMethod.GetGenericArguments();
            var targetGenericArguments = targetMethod.GetGenericArguments();
            Assert.Equal(interfaceGenericArguments.Length, targetGenericArguments.Length);
            for (var j = 0; j < interfaceGenericArguments.Length; j++)
            {
                var interfaceMembers = interfaceGenericArguments[j].GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
                var targetMembers = targetGenericArguments[j].GetCustomAttribute<DynamicallyAccessedMembersAttribute>();

                Assert.NotNull(interfaceMembers);
                Assert.NotNull(targetMembers);
                Assert.Equal(interfaceMembers.MemberTypes, targetMembers.MemberTypes);
            }
        }
    }
}
