// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class SurrogateParameterInfoTests
{
    [Fact]
    public void Initialization_SetsTypeAndNameFromPropertyInfo()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.Equal(propertyInfo.Name, surrogateParameterInfo.Name);
        Assert.Equal(propertyInfo.PropertyType, surrogateParameterInfo.ParameterType);
    }

    [Fact]
    public void Initialization_WithConstructorArgument_SetsTypeAndNameFromPropertyInfo()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.Equal(propertyInfo.Name, surrogateParameterInfo.Name);
        Assert.Equal(propertyInfo.PropertyType, surrogateParameterInfo.ParameterType);
    }

    [Fact]
    public void SurrogateParameterInfo_ContainsPropertyCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Act & Assert
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_UsesParameterCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithTestAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Act & Assert
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_FallbackToPropertyCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Act & Assert
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    [Fact]
    public void SurrogateParameterInfo_ContainsPropertyCustomAttributesData()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Act
        var attributes = surrogateParameterInfo.GetCustomAttributesData();

        // Assert
        Assert.Single(
            attributes,
            a => typeof(TestAttribute).IsAssignableFrom(a.AttributeType));
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_MergePropertyAndParameterCustomAttributesData()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithSampleAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Act
        var attributes = surrogateParameterInfo.GetCustomAttributesData();

        // Assert
        Assert.Single(
            surrogateParameterInfo.GetCustomAttributesData(),
            a => typeof(TestAttribute).IsAssignableFrom(a.AttributeType));
        Assert.Single(
            surrogateParameterInfo.GetCustomAttributesData(),
            a => typeof(SampleAttribute).IsAssignableFrom(a.AttributeType));
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_MergePropertyAndParameterCustomAttributes()
    {
        // Arrange
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.WithTestAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.WithSampleAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Act
        var attributes = surrogateParameterInfo.GetCustomAttributes(true);

        // Assert
        Assert.Single(
            attributes,
            a => typeof(TestAttribute).IsAssignableFrom(a.GetType()));
        Assert.Single(
            attributes,
            a => typeof(SampleAttribute).IsAssignableFrom(a.GetType()));
    }

    [Fact]
    public void SurrogateParameterInfo_ContainsPropertyInheritedCustomAttributes()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(DerivedArgumentList), nameof(DerivedArgumentList.WithTestAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute), true));
    }

    [Fact]
    public void SurrogateParameterInfo_DoesNotHaveDefaultValueFromProperty()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.False(surrogateParameterInfo.HasDefaultValue);
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_HasDefaultValue()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), "withDefaultValue");
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.True(surrogateParameterInfo.HasDefaultValue);
        Assert.NotNull(surrogateParameterInfo.DefaultValue);
        Assert.IsType<int>(surrogateParameterInfo.DefaultValue);
        Assert.NotNull(surrogateParameterInfo.RawDefaultValue);
        Assert.IsType<int>(surrogateParameterInfo.RawDefaultValue);
    }

    [Fact]
    public void SurrogateParameterInfo_WithConstructorArgument_DoesNotHaveDefaultValue()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var parameter = GetParameter(nameof(ArgumentList.DefaultMethod), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo, parameter);

        // Assert
        Assert.False(surrogateParameterInfo.HasDefaultValue);
    }

    private static PropertyInfo GetProperty(Type containerType, string propertyName)
        => containerType.GetProperty(propertyName);

    private static ParameterInfo GetParameter(string methodName, string parameterName)
    {
        var methodInfo = typeof(ArgumentList).GetMethod(methodName);
        var parameters = methodInfo.GetParameters();
        return parameters.Single(p => p.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
    }

    private class ArgumentList
    {
        public int NoAttribute { get; set; }

        [Test]
        public virtual int WithTestAttribute { get; set; }

        [Sample]
        public int WithSampleAttribute { get; set; }

        public void DefaultMethod(
            int noAttribute,
            [Test] int withTestAttribute,
            [Sample] int withSampleAttribute,
            int withDefaultValue = 10)
        { }
    }

    private class DerivedArgumentList : ArgumentList
    {
        [DerivedTest]
        public override int WithTestAttribute
        {
            get => base.WithTestAttribute;
            set => base.WithTestAttribute = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
    private class SampleAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
    private class TestAttribute : Attribute
    { }

    private class DerivedTestAttribute : TestAttribute
    { }
}
