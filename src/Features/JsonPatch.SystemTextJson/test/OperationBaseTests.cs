// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

public class OperationBaseTests
{
    [Fact]
    public void ShouldSerializeFrom_HasObsoleteAttribute()
    {
        const string expectedMessage = "This method is obsolete and will be removed in .NET 13. If you were calling this method, replace the call with 'operation.OperationType is OperationType.Move or OperationType.Copy'";

        var method = typeof(OperationBase).GetMethod(nameof(OperationBase.ShouldSerializeFrom));
        Assert.NotNull(method);

        var obsoleteAttributes = method.GetCustomAttributes(typeof(System.ObsoleteAttribute), inherit: false);
        var obsoleteAttribute = Assert.IsType<System.ObsoleteAttribute>(Assert.Single(obsoleteAttributes));
        Assert.Equal(expectedMessage, obsoleteAttribute.Message);
    }

    [Theory]
    [InlineData("ADd", OperationType.Add)]
    [InlineData("Copy", OperationType.Copy)]
    [InlineData("mOVE", OperationType.Move)]
    [InlineData("REMOVE", OperationType.Remove)]
    [InlineData("replace", OperationType.Replace)]
    [InlineData("TeSt", OperationType.Test)]
    public void SetValidOperationType(string op, OperationType operationType)
    {
        // Arrange
        var operationBase = new OperationBase();
        operationBase.op = op;

        // Act & Assert
        Assert.Equal(operationType, operationBase.OperationType);
    }

    [Theory]
    [InlineData("invalid", OperationType.Invalid)]
    [InlineData("coppy", OperationType.Invalid)]
    [InlineData("notvalid", OperationType.Invalid)]
    public void InvalidOperationType_SetsOperationTypeInvalid(string op, OperationType operationType)
    {
        // Arrange
        var operationBase = new OperationBase();
        operationBase.op = op;

        // Act & Assert
        Assert.Equal(operationType, operationBase.OperationType);
    }
}
