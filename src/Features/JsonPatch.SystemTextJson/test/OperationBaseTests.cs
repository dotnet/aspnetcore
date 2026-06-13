// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

public class OperationBaseTests
{
	[Xunit.Theory]
	[Xunit.InlineData("ADd", OperationType.Add)]
	[Xunit.InlineData("Copy", OperationType.Copy)]
	[Xunit.InlineData("mOVE", OperationType.Move)]
	[Xunit.InlineData("REMOVE", OperationType.Remove)]
	[Xunit.InlineData("replace", OperationType.Replace)]
	[Xunit.InlineData("TeSt", OperationType.Test)]
	public void SetValidOperationType(string op, OperationType operationType)
	{
		// Arrange
		var operationBase = new OperationBase();
		operationBase.op = op;

		// Act & Assert
		Xunit.Assert.Equal(operationType, operationBase.OperationType);
	}

	[Xunit.Theory]
	[Xunit.InlineData("invalid", OperationType.Invalid)]
	[Xunit.InlineData("coppy", OperationType.Invalid)]
	[Xunit.InlineData("notvalid", OperationType.Invalid)]
	public void InvalidOperationType_SetsOperationTypeInvalid(string op, OperationType operationType)
	{
		// Arrange
		var operationBase = new OperationBase();
		operationBase.op = op;

		// Act & Assert
		Xunit.Assert.Equal(operationType, operationBase.OperationType);
	}
}
