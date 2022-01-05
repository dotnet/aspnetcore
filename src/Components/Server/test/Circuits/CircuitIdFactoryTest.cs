// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class circuitIdFactoryTest
{
    [Fact]
    public void CreateCircuitId_Generates_NewRandomId()
    {
        var factory = TestCircuitIdFactory.CreateTestFactory();

        // Act
        var secret = factory.CreateCircuitId();

        // Assert
        Assert.NotNull(secret.Secret);
        // This is the magic data protection header that validates its protected
        Assert.StartsWith("CfDJ", secret.Secret);
    }

    [Fact]
    public void CreateCircuitId_Generates_GeneratesDifferentIds_ForSuccessiveCalls()
    {
        // Arrange
        var factory = TestCircuitIdFactory.CreateTestFactory();

        // Act
        var secrets = Enumerable.Range(0, 100).Select(i => factory.CreateCircuitId()).Select(s => s.Secret).ToArray();

        // Assert
        Assert.All(secrets, secret => Assert.NotNull(secret));
        Assert.Equal(100, secrets.Distinct(StringComparer.Ordinal).Count());
    }

    // Note that this test also verifies that the ID can be reproduced from the secret.
    [Fact]
    public void CircuitIds_Roundtrip()
    {
        // Arrange
        var factory = TestCircuitIdFactory.CreateTestFactory();
        var id = factory.CreateCircuitId();

        // Act
        var isValid = factory.TryParseCircuitId(id.Secret, out var parsed);

        // Assert
        Assert.True(isValid, "Failed to validate id");
        Assert.Equal(id, parsed);
        Assert.Equal(id.Secret, parsed.Secret);
        Assert.Equal(id.Id, parsed.Id);
    }

    [Fact]
    public void ValidateCircuitId_ReturnsFalseForMalformedPayloads()
    {
        // Arrange
        var factory = TestCircuitIdFactory.CreateTestFactory();

        // Act
        var isValid = factory.TryParseCircuitId("$%@&==", out _);

        // Assert
        Assert.False(isValid, "Accepted an invalid payload");
    }

    [Fact]
    public void ValidateCircuitId_ReturnsFalseForPotentiallyTamperedPayloads()
    {
        // Arrange
        var factory = TestCircuitIdFactory.CreateTestFactory();
        var secret = factory.CreateCircuitId();
        var protectedBytes = Base64UrlTextEncoder.Decode(secret.Secret);
        for (int i = protectedBytes.Length - 10; i < protectedBytes.Length; i++)
        {
            protectedBytes[i] = 0;
        }
        var tampered = Base64UrlTextEncoder.Encode(protectedBytes);

        // Act
        var isValid = factory.TryParseCircuitId(tampered, out _);

        // Assert
        Assert.False(isValid, "Accepted a tampered payload");
    }
}
