// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Web;

public class JSComponentConfigurationExtensionsTest
{
    [Fact]
    public void RegisterForJavaScriptMethods_RequireUnreferencedCode()
    {
        var methods = typeof(JSComponentConfigurationExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == nameof(JSComponentConfigurationExtensions.RegisterForJavaScript))
            .ToArray();

        Assert.Equal(4, methods.Length);
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal("A registered component might serialize arbitrary EventCallback<T> values whose types cannot be statically analyzed.", attribute.Message);
        }
    }
}
