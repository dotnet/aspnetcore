// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Operations;

public class OperationBaseTests
{
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
