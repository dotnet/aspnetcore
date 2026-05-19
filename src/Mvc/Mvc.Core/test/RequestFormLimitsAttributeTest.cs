// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Mvc;

public class RequestFormLimitsAttributeTest
{
    [Fact]
    public void AllPublicProperties_OfFormOptions_AreExposed()
    {
        // Arrange
        var formOptionsProperties = GetProperties(typeof(FormOptions));
        var formLimitsAttributeProperties = GetProperties(typeof(RequestFormLimitsAttribute));

        // Act & Assert
        foreach (var property in formOptionsProperties)
        {
            var formLimitAttributeProperty = formLimitsAttributeProperties
                .Where(pi => property.Name == pi.Name && pi.PropertyType == property.PropertyType)
                .SingleOrDefault();
            Assert.NotNull(formLimitAttributeProperty);
        }
    }

    [Fact]
    public void CreatesFormOptions_WithDefaults()
    {
        // Arrange
        var formOptionsProperties = GetProperties(typeof(FormOptions));
        var formLimitsAttributeProperties = GetProperties(typeof(RequestFormLimitsAttribute));
        var formOptions = new FormOptions();

        // Act
        var requestFormLimitsAttribute = new RequestFormLimitsAttribute();

        // Assert
        foreach (var formOptionsProperty in formOptionsProperties)
        {
            var formLimitsAttributeProperty = formLimitsAttributeProperties
                .Where(pi => pi.Name == formOptionsProperty.Name && pi.PropertyType == formOptionsProperty.PropertyType)
                .SingleOrDefault();

            Assert.Equal(
                formOptionsProperty.GetValue(formOptions),
                formLimitsAttributeProperty.GetValue(requestFormLimitsAttribute));
        }
    }

    [Fact]
    public void UpdatesFormOptions_WithOverridenValues()
    {
        // Arrange
        var requestFormLimitsAttribute = new RequestFormLimitsAttribute();

        // Act
        requestFormLimitsAttribute.BufferBody = true;
        requestFormLimitsAttribute.BufferBodyLengthLimit = 0;
        requestFormLimitsAttribute.KeyLengthLimit = 0;
        requestFormLimitsAttribute.MemoryBufferThreshold = 0;
        requestFormLimitsAttribute.MultipartBodyLengthLimit = 0;
        requestFormLimitsAttribute.MultipartBoundaryLengthLimit = 0;
        requestFormLimitsAttribute.MultipartHeadersCountLimit = 0;
        requestFormLimitsAttribute.MultipartHeadersLengthLimit = 0;
        requestFormLimitsAttribute.ValueCountLimit = 0;
        requestFormLimitsAttribute.ValueLengthLimit = 0;

        // Assert
        Assert.True(requestFormLimitsAttribute.FormOptions.BufferBody);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.BufferBodyLengthLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.KeyLengthLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.MemoryBufferThreshold);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.MultipartBodyLengthLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.MultipartBoundaryLengthLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.MultipartHeadersCountLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.MultipartHeadersLengthLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.ValueCountLimit);
        Assert.Equal(0, requestFormLimitsAttribute.FormOptions.ValueLengthLimit);
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }
}
