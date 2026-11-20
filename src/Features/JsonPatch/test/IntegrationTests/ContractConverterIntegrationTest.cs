// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class ContractConverterIntegrationTest
{
    [Fact]
    public void Copy_UsesDocumentContractResolver()
    {
        // Arrange
        var targetObject = new ContractConverterObject
        {
            Code = new Code("ABCD1234")
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("Code", "BackupCode");
        patchDocument.ContractResolver = new CodeContractResolver();

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal("ABCD1234", targetObject.Code.ToString());
        Assert.Equal("ABCD1234", targetObject.BackupCode.ToString());
    }

    [Fact]
    public void Test_UsesDocumentContractResolver()
    {
        // Arrange
        var targetObject = new ContractConverterObject
        {
            Code = new Code("ABCD1234")
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Test("Code", "ZZZZZZZZ");
        patchDocument.ContractResolver = new CodeContractResolver();

        // Act
        var exception = Assert.Throws<JsonPatchException>(() => patchDocument.ApplyTo(targetObject));

        // Assert
        Assert.Equal(
            "The current value 'ABCD1234' at path 'Code' is not equal to the test value 'ZZZZZZZZ'.",
            exception.Message);
    }
}
