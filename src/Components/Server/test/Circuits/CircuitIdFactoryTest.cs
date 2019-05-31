// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits
{
    public class CircuitIdFactoryTest
    {
        [Fact]
        public void CreateCircuitId_Generates_NewRandomId()
        {
            CircuitIdFactory factory = CreateTestFactory();

            // Act
            var id = factory.CreateCircuitId();

            // Assert
            Assert.NotNull(id);
            // This is the magic data protection header that validates its protected
            Assert.StartsWith("CfDJ", id);
        }

        [Fact]
        public void CreateCircuitId_Generates_GeneratesDifferentIds_ForSuccesiveCalls()
        {
            // Arrange
            CircuitIdFactory factory = CreateTestFactory();

            // Act
            var ids = Enumerable.Range(0, 100).Select(i => factory.CreateCircuitId()).ToArray();

            // Assert
            Assert.All(ids, id => Assert.NotNull(id));
            Assert.Equal(100, ids.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void CircuitIds_Roundtrip()
        {
            // Arrange
            CircuitIdFactory factory = CreateTestFactory();
            var id = factory.CreateCircuitId();

            // Act
            var isValid = factory.ValidateCircuitId(id);

            // Assert
            Assert.True(isValid, "Failed to validate id");
        }

        [Fact]
        public void ValidateCircuitId_ReturnsFalseForMalformedPayloads()
        {
            // Arrange
            CircuitIdFactory factory = CreateTestFactory();

            // Act
            var isValid = factory.ValidateCircuitId("$%@&==");

            // Assert
            Assert.False(isValid, "Accepted an invalid payload");
        }

        [Fact]
        public void ValidateCircuitId_ReturnsFalseForPotentiallyTamperedPayloads()
        {
            // Arrange
            CircuitIdFactory factory = CreateTestFactory();
            var id = factory.CreateCircuitId();
            var protectedBytes = Base64UrlTextEncoder.Decode(id);
            for (int i = protectedBytes.Length - 10; i < protectedBytes.Length; i++)
            {
                protectedBytes[i] = 0;
            }
            var tamperedId = Base64UrlTextEncoder.Encode(protectedBytes);

            // Act
            var isValid = factory.ValidateCircuitId(tamperedId);

            // Assert
            Assert.False(isValid, "Accepted a tampered payload");
        }

        private static CircuitIdFactory CreateTestFactory()
        {
            // Arrange
            return new CircuitIdFactory(Options.Create(new CircuitOptions
            {
                CircuitIdProtector = new EphemeralDataProtectionProvider().CreateProtector("Test")
            }));
        }
    }
}
