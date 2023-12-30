// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection;

public class TypeForwardingActivatorTests : MarshalByRefObject
{
    [Fact]
    public void CreateInstance_ForwardsToNewNamespaceIfExists()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var name = "Microsoft.AspNet.DataProtection.TypeForwardingActivatorTests+ClassWithParameterlessCtor, Microsoft.AspNet.DataProtection.Tests, Version=1.0.0.0";
        var instance = activator.CreateInstance<object>(name);

        // Assert
        Assert.IsType<ClassWithParameterlessCtor>(instance);
    }

    [Fact]
    public void CreateInstance_DoesNotForwardIfClassDoesNotExist()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act & Assert
        var name = "Microsoft.AspNet.DataProtection.TypeForwardingActivatorTests+NonExistentClassWithParameterlessCtor, Microsoft.AspNet.DataProtection.Tests";
        var exception = Assert.ThrowsAny<Exception>(() => activator.CreateInstance<object>(name));

        Assert.Contains("Microsoft.AspNet.DataProtection.Test", exception.Message);
    }

    [Theory]
    [InlineData(typeof(GenericType<GenericType<ClassWithParameterlessCtor>>))]
    [InlineData(typeof(GenericType<ClassWithParameterlessCtor>))]
    [InlineData(typeof(GenericType<GenericType<string>>))]
    [InlineData(typeof(GenericType<GenericType<string, string>>))]
    [InlineData(typeof(GenericType<string>))]
    [InlineData(typeof(GenericType<int>))]
    [InlineData(typeof(List<ClassWithParameterlessCtor>))]
    public void CreateInstance_Generics(Type type)
    {
        // Arrange
        var activator = new TypeForwardingActivator(null);
        var name = type.AssemblyQualifiedName;

        // Act & Assert
        Assert.IsType(type, activator.CreateInstance<object>(name));
    }

    [Theory]
    [InlineData(typeof(GenericType<>))]
    [InlineData(typeof(GenericType<,>))]
    public void CreateInstance_ThrowsForOpenGenerics(Type type)
    {
        // Arrange
        var activator = new TypeForwardingActivator(null);
        var name = type.AssemblyQualifiedName;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => activator.CreateInstance<object>(name));
    }

    [Theory]
    [InlineData(
        "System.Tuple`1[[Some.Type, Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Tuple`1[[Some.Type, Microsoft.AspNetCore.DataProtection, Culture=neutral]], mscorlib, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [InlineData(
        "Some.Type`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral",
        "Some.Type`1[[System.Int32, mscorlib, Culture=neutral, PublicKeyToken=b77a5c561934e089]], Microsoft.AspNetCore.DataProtection, Culture=neutral")]
    [InlineData(
        "System.Tuple`1[[System.Tuple`1[[Some.Type, Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
        "System.Tuple`1[[System.Tuple`1[[Some.Type, Microsoft.AspNetCore.DataProtection, Culture=neutral]], mscorlib, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public void ParsesFullyQualifiedTypeName(string typeName, string expected)
    {
        Assert.Equal(expected, new MockTypeForwardingActivator().Parse(typeName));
    }

    [Theory]
    [InlineData(typeof(List<string>))]
    [InlineData(typeof(FactAttribute))]
    public void CreateInstance_DoesNotForwardingTypesExternalTypes(Type type)
    {
        new TypeForwardingActivator(null).CreateInstance(typeof(object), type.AssemblyQualifiedName, out var forwarded);
        Assert.False(forwarded, "Should not have forwarded types that are not in Microsoft.AspNetCore.DataProjection");
    }

    [Theory]
    [MemberData(nameof(AssemblyVersions))]
    public void CreateInstance_ForwardsAcrossVersionChanges(Version version)
    {
        CreateInstance_ForwardsAcrossVersionChangesImpl(version);
    }

    private void CreateInstance_ForwardsAcrossVersionChangesImpl(Version newVersion)
    {
        var activator = new TypeForwardingActivator(null);

        var type = typeof(ClassWithParameterlessCtor);
        var typeName = type.FullName;
        var assemblyName = type.Assembly.GetName();

        assemblyName.Version = newVersion;
        var newName = $"{typeName}, {assemblyName}";

        Assert.NotEqual(type.AssemblyQualifiedName, newName);
        Assert.IsType<ClassWithParameterlessCtor>(activator.CreateInstance(typeof(object), newName, out var forwarded));
        Assert.True(forwarded, "Should have forwarded this type to new version or namespace");
    }

    public static TheoryData<Version> AssemblyVersions
    {
        get
        {
            var current = typeof(ActivatorTests).Assembly.GetName().Version;
            return new TheoryData<Version>
                {
                    new Version(Math.Max(0, current.Major - 1), 0, 0, 0),
                    new Version(current.Major + 1, 0, 0, 0),
                    new Version(current.Major, current.Minor + 1, 0, 0),
                    new Version(current.Major, current.Minor, current.Build + 1, 0),
                };
        }
    }

    private class MockTypeForwardingActivator : TypeForwardingActivator
    {
        public MockTypeForwardingActivator() : base(null) { }
        public string Parse(string typeName) => RemoveVersionFromAssemblyName(typeName);
    }

    private class ClassWithParameterlessCtor
    {
    }

    private class GenericType<T>
    {
    }

    private class GenericType<T1, T2>
    {
    }
}
