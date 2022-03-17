// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

public class DataMemberRequiredBindingMetadataProviderTest
{
    [Fact]
    public void IsBindingRequired_SetToTrue_WithDataMemberIsRequiredTrue()
    {
        // Arrange
        var provider = new DataMemberRequiredBindingMetadataProvider();

        var attributes = new object[]
        {
                new DataMemberAttribute() { IsRequired = true, }
        };

        var key = ModelMetadataIdentity.ForProperty(
            typeof(ClassWithDataMemberIsRequiredTrue).GetProperty(nameof(ClassWithDataMemberIsRequiredTrue.StringProperty)),
            typeof(string),
            typeof(ClassWithDataMemberIsRequiredTrue));
        var context = new BindingMetadataProviderContext(key, GetModelAttributes(new object[0], attributes));

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.True(context.BindingMetadata.IsBindingRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingRequired_LeftAlone_DataMemberIsRequiredFalse(bool initialValue)
    {
        // Arrange
        var provider = new DataMemberRequiredBindingMetadataProvider();

        var attributes = new object[]
        {
                new DataMemberAttribute() { IsRequired = false, }
        };

        var key = ModelMetadataIdentity.ForProperty(
            typeof(ClassWithDataMemberIsRequiredFalse).GetProperty(nameof(ClassWithDataMemberIsRequiredFalse.StringProperty)),
            typeof(string),
            typeof(ClassWithDataMemberIsRequiredFalse));
        var context = new BindingMetadataProviderContext(key, GetModelAttributes(new object[0], attributes));

        context.BindingMetadata.IsBindingRequired = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingRequired_LeftAlone_ForNonPropertyMetadata(bool initialValue)
    {
        // Arrange
        var provider = new DataMemberRequiredBindingMetadataProvider();

        var attributes = new object[]
        {
                new DataMemberAttribute() { IsRequired = true, }
        };

        var key = ModelMetadataIdentity.ForType(typeof(ClassWithDataMemberIsRequiredTrue));
        var context = new BindingMetadataProviderContext(key, GetModelAttributes(new object[0], attributes));

        context.BindingMetadata.IsBindingRequired = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingRequired_LeftAlone_WithoutDataMemberAttribute(bool initialValue)
    {
        // Arrange
        var provider = new DataMemberRequiredBindingMetadataProvider();

        var key = ModelMetadataIdentity.ForProperty(
            typeof(ClassWithoutAttributes).GetProperty(nameof(ClassWithoutAttributes.StringProperty)),
            typeof(string),
            typeof(ClassWithoutAttributes));
        var context = new BindingMetadataProviderContext(key, GetModelAttributes(new object[0], new object[0]));

        context.BindingMetadata.IsBindingRequired = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingRequired_LeftAlone_WithoutDataContractAttribute(bool initialValue)
    {
        // Arrange
        var provider = new DataMemberRequiredBindingMetadataProvider();

        var attributes = new object[]
        {
                new DataMemberAttribute() { IsRequired = true, }
        };

        var key = ModelMetadataIdentity.ForProperty(
            typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract).GetProperty(nameof(ClassWithDataMemberIsRequiredTrueWithoutDataContract.StringProperty)),
            typeof(string),
            typeof(ClassWithDataMemberIsRequiredTrueWithoutDataContract));
        var context = new BindingMetadataProviderContext(key, GetModelAttributes(new object[0], attributes));

        context.BindingMetadata.IsBindingRequired = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingRequired);
    }

    private ModelAttributes GetModelAttributes(
        IEnumerable<object> typeAttributes,
        IEnumerable<object> propertyAttributes)
        => new ModelAttributes(typeAttributes, propertyAttributes, Array.Empty<object>());

    [DataContract]
    private class ClassWithDataMemberIsRequiredTrue
    {
        [DataMember(IsRequired = true)]
        public string StringProperty { get; set; }
    }

    [DataContract]
    private class ClassWithDataMemberIsRequiredFalse
    {
        [DataMember(IsRequired = false)]
        public string StringProperty { get; set; }
    }

    private class ClassWithDataMemberIsRequiredTrueWithoutDataContract
    {
        [DataMember(IsRequired = true)]
        public string StringProperty { get; set; }
    }

    private class ClassWithoutAttributes
    {
        public string StringProperty { get; set; }
    }
}
