// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Identity.Test;

public abstract class ApiConsistencyTestBase
{
    [Fact]
    public void Public_inheritable_apis_should_be_virtual()
    {
        var nonVirtualMethods
            = (from type in GetAllTypes(TargetAssembly.DefinedTypes)
               where type.IsVisible
                     && !type.IsSealed
                     && type.DeclaredConstructors.Any(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly)
                     && type.Namespace != null
                     && !type.Namespace.EndsWith(".Compiled", StringComparison.Ordinal)
               from method in type.DeclaredMethods.Where(m => m.IsPublic && !m.IsStatic)
               where GetBasestTypeInAssembly(method.DeclaringType) == type
                     && !(method.IsVirtual && !method.IsFinal)
                     && !method.Name.StartsWith("get_", StringComparison.Ordinal)
                     && !method.Name.StartsWith("set_", StringComparison.Ordinal)
                     && !method.Name.Equals("Dispose")
               select type.Name + "." + method.Name)
                .ToList();

        Assert.False(
            nonVirtualMethods.Any(),
            "\r\n-- Missing virtual APIs --\r\n" + string.Join("\r\n", nonVirtualMethods));
    }

    [Fact]
    public void Async_methods_should_end_with_async_suffix()
    {
        var asyncMethods
            = (from type in GetAllTypes(TargetAssembly.DefinedTypes)
               where type.IsVisible
               from method in type.DeclaredMethods.Where(m => m.IsPublic)
               where GetBasestTypeInAssembly(method.DeclaringType) == type
               where typeof(Task).IsAssignableFrom(method.ReturnType)
               select method).ToList();

        var missingSuffixMethods
            = asyncMethods
                .Where(method => !method.Name.EndsWith("Async", StringComparison.Ordinal))
                .Select(method => method.DeclaringType.Name + "." + method.Name)
                .Except(GetAsyncSuffixExceptions())
                .ToList();

        Assert.False(
            missingSuffixMethods.Any(),
            "\r\n-- Missing async suffix --\r\n" + string.Join("\r\n", missingSuffixMethods));
    }

    protected virtual IEnumerable<string> GetCancellationTokenExceptions()
    {
        return Enumerable.Empty<string>();
    }

    protected virtual IEnumerable<string> GetAsyncSuffixExceptions()
    {
        return Enumerable.Empty<string>();
    }

    protected abstract Assembly TargetAssembly { get; }

    protected virtual IEnumerable<TypeInfo> GetAllTypes(IEnumerable<TypeInfo> types)
    {
        foreach (var type in types)
        {
            yield return type;

            foreach (var nestedType in GetAllTypes(type.DeclaredNestedTypes))
            {
                yield return nestedType;
            }
        }
    }

    protected Type GetBasestTypeInAssembly(Type type)
    {
        while (type.BaseType?.Assembly == type.Assembly)
        {
            type = type.BaseType;
        }

        return type;
    }
}
