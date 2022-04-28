// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

internal class SurrogateParameterInfoTests
{
    public void Initialization_SetsTypeAndNameFromPropertyInfo()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.NoAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.Equal(propertyInfo.Name, surrogateParameterInfo.Name);
        Assert.Equal(propertyInfo.PropertyType, surrogateParameterInfo.ParameterType);
    }

    public void SurrogateParameterInfo_ContainsPropertyCustomAttributes()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(ArgumentList), nameof(ArgumentList.PropertyWithCustomAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.Equal(propertyInfo.CustomAttributes, surrogateParameterInfo.CustomAttributes);
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    public void SurrogateParameterInfo_ContainsPropertyInheritedCustomAttributes()
    {
        // Arrange & Act
        var propertyInfo = GetProperty(typeof(DerivedArgumentList), nameof(DerivedArgumentList.PropertyWithCustomAttribute));
        var surrogateParameterInfo = new SurrogateParameterInfo(propertyInfo);

        // Assert
        Assert.Equal(propertyInfo.CustomAttributes, surrogateParameterInfo.CustomAttributes);
        Assert.Single(surrogateParameterInfo.GetCustomAttributes(typeof(TestAttribute)));
    }

    private static PropertyInfo GetProperty(Type containerType, string propertyName)
        => containerType.GetProperty(propertyName);

    private class ArgumentList
    {
        public int NoAttribute { get; set; }

        [TestAttribute]
        public virtual int PropertyWithCustomAttribute { get; set; }
    }

    private class DerivedArgumentList : ArgumentList
    {
        public override int PropertyWithCustomAttribute
        {
            get => base.PropertyWithCustomAttribute;
            set => base.PropertyWithCustomAttribute = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    private class TestAttribute : Attribute
    { }
}
