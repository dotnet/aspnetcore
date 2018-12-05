// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class PasswordHasherTest
    {
        [Fact]
        public void Ctor_InvalidCompatMode_Throws()
        {
            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                new PasswordHasher(compatMode: (PasswordHasherCompatibilityMode)(-1));
            });
            Assert.Equal("The provided PasswordHasherCompatibilityMode is invalid.", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Ctor_InvalidIterCount_Throws(int iterCount)
        {
            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                new PasswordHasher(iterCount: iterCount);
            });
            Assert.Equal("The iteration count must be a positive integer.", ex.Message);
        }

        [Theory]
        [InlineData(PasswordHasherCompatibilityMode.IdentityV2)]
        [InlineData(PasswordHasherCompatibilityMode.IdentityV3)]
        public void FullRoundTrip(PasswordHasherCompatibilityMode compatMode)
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: compatMode);

            // Act & assert - success case
            var hashedPassword = hasher.HashPassword(null, "password 1");
            var successResult = hasher.VerifyHashedPassword(null, hashedPassword, "password 1");
            Assert.Equal(PasswordVerificationResult.Success, successResult);

            // Act & assert - failure case
            var failedResult = hasher.VerifyHashedPassword(null, hashedPassword, "password 2");
            Assert.Equal(PasswordVerificationResult.Failed, failedResult);
        }

        [Fact]
        public void HashPassword_DefaultsToVersion3()
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: null);

            // Act
            string retVal = hasher.HashPassword(null, "my password");

            // Assert
            Assert.Equal("AQAAAAEAACcQAAAAEAABAgMEBQYHCAkKCwwNDg+yWU7rLgUwPZb1Itsmra7cbxw2EFpwpVFIEtP+JIuUEw==", retVal);
        }

        [Fact]
        public void HashPassword_Version2()
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV2);

            // Act
            string retVal = hasher.HashPassword(null, "my password");

            // Assert
            Assert.Equal("AAABAgMEBQYHCAkKCwwNDg+ukCEMDf0yyQ29NYubggHIVY0sdEUfdyeM+E1LtH1uJg==", retVal);
        }

        [Fact]
        public void HashPassword_Version3()
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV3);

            // Act
            string retVal = hasher.HashPassword(null, "my password");

            // Assert
            Assert.Equal("AQAAAAEAACcQAAAAEAABAgMEBQYHCAkKCwwNDg+yWU7rLgUwPZb1Itsmra7cbxw2EFpwpVFIEtP+JIuUEw==", retVal);
        }

        [Theory]
        // Version 2 payloads
        [InlineData("AAABAgMEBQYHCAkKCwwNDg+uAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAALtH1uJg==")] // incorrect password
        [InlineData("AAABAgMEBQYHCAkKCwwNDg+ukCEMDf0yyQ29NYubggE=")] // too short
        [InlineData("AAABAgMEBQYHCAkKCwwNDg+ukCEMDf0yyQ29NYubggHIVY0sdEUfdyeM+E1LtH1uJgAAAAAAAAAAAAA=")] // extra data at end
        // Version 3 payloads
        [InlineData("AQAAAAAAAAD6AAAAEAhftMyfTJyAAAAAAAAAAAAAAAAAAAih5WsjXaR3PA9M")] // incorrect password
        [InlineData("AQAAAAIAAAAyAAAAEOMwvh3+FZxqkdMBz2ekgGhwQ4A=")] // too short
        [InlineData("AQAAAAIAAAAyAAAAEOMwvh3+FZxqkdMBz2ekgGhwQ4B6pZWND6zgESBuWiHwAAAAAAAAAAAA")] // extra data at end
        public void VerifyHashedPassword_FailureCases(string hashedPassword)
        {
            // Arrange
            var hasher = new PasswordHasher();

            // Act
            var result = hasher.VerifyHashedPassword(null, hashedPassword, "my password");

            // Assert
            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Theory]
        // Version 2 payloads
        [InlineData("ANXrDknc7fGPpigibZXXZFMX4aoqz44JveK6jQuwY3eH/UyPhvr5xTPeGYEckLxz9A==")] // SHA1, 1000 iterations, 128-bit salt, 256-bit subkey
        // Version 3 payloads
        [InlineData("AQAAAAIAAAAyAAAAEOMwvh3+FZxqkdMBz2ekgGhwQ4B6pZWND6zgESBuWiHw")] // SHA512, 50 iterations, 128-bit salt, 128-bit subkey
        [InlineData("AQAAAAIAAAD6AAAAIJbVi5wbMR+htSfFp8fTw8N8GOS/Sje+S/4YZcgBfU7EQuqv4OkVYmc4VJl9AGZzmRTxSkP7LtVi9IWyUxX8IAAfZ8v+ZfhjCcudtC1YERSqE1OEdXLW9VukPuJWBBjLuw==")] // SHA512, 250 iterations, 256-bit salt, 512-bit subkey
        [InlineData("AQAAAAAAAAD6AAAAEAhftMyfTJylOlZT+eEotFXd1elee8ih5WsjXaR3PA9M")] // SHA1, 250 iterations, 128-bit salt, 128-bit subkey
        [InlineData("AQAAAAEAA9CQAAAAIESkQuj2Du8Y+kbc5lcN/W/3NiAZFEm11P27nrSN5/tId+bR1SwV8CO1Jd72r4C08OLvplNlCDc3oQZ8efcW+jQ=")] // SHA256, 250000 iterations, 256-bit salt, 256-bit subkey
        public void VerifyHashedPassword_Version2CompatMode_SuccessCases(string hashedPassword)
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV2);

            // Act
            var result = hasher.VerifyHashedPassword(null, hashedPassword, "my password");

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Theory]
        // Version 2 payloads
        [InlineData("ANXrDknc7fGPpigibZXXZFMX4aoqz44JveK6jQuwY3eH/UyPhvr5xTPeGYEckLxz9A==", PasswordVerificationResult.SuccessRehashNeeded)] // SHA1, 1000 iterations, 128-bit salt, 256-bit subkey
        // Version 3 payloads
        [InlineData("AQAAAAIAAAAyAAAAEOMwvh3+FZxqkdMBz2ekgGhwQ4B6pZWND6zgESBuWiHw", PasswordVerificationResult.SuccessRehashNeeded)] // SHA512, 50 iterations, 128-bit salt, 128-bit subkey
        [InlineData("AQAAAAIAAAD6AAAAIJbVi5wbMR+htSfFp8fTw8N8GOS/Sje+S/4YZcgBfU7EQuqv4OkVYmc4VJl9AGZzmRTxSkP7LtVi9IWyUxX8IAAfZ8v+ZfhjCcudtC1YERSqE1OEdXLW9VukPuJWBBjLuw==", PasswordVerificationResult.SuccessRehashNeeded)] // SHA512, 250 iterations, 256-bit salt, 512-bit subkey
        [InlineData("AQAAAAAAAAD6AAAAEAhftMyfTJylOlZT+eEotFXd1elee8ih5WsjXaR3PA9M", PasswordVerificationResult.SuccessRehashNeeded)] // SHA1, 250 iterations, 128-bit salt, 128-bit subkey
        [InlineData("AQAAAAEAA9CQAAAAIESkQuj2Du8Y+kbc5lcN/W/3NiAZFEm11P27nrSN5/tId+bR1SwV8CO1Jd72r4C08OLvplNlCDc3oQZ8efcW+jQ=", PasswordVerificationResult.Success)] // SHA256, 250000 iterations, 256-bit salt, 256-bit subkey
        public void VerifyHashedPassword_Version3CompatMode_SuccessCases(string hashedPassword, PasswordVerificationResult expectedResult)
        {
            // Arrange
            var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV3);

            // Act
            var actualResult = hasher.VerifyHashedPassword(null, hashedPassword, "my password");

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        private sealed class PasswordHasher : PasswordHasher<object>
        {
            public PasswordHasher(PasswordHasherCompatibilityMode? compatMode = null, int? iterCount = null)
                : base(BuildOptions(compatMode, iterCount))
            {
            }

            private static IOptions<PasswordHasherOptions> BuildOptions(PasswordHasherCompatibilityMode? compatMode, int? iterCount)
            {
                var options = new PasswordHasherOptionsAccessor();
                if (compatMode != null)
                {
                    options.Value.CompatibilityMode = (PasswordHasherCompatibilityMode)compatMode;
                }
                if (iterCount != null)
                {
                    options.Value.IterationCount = (int)iterCount;
                }
                Assert.NotNull(options.Value.Rng); // should have a default value
                options.Value.Rng = new SequentialRandomNumberGenerator();
                return options;
            }
        }

        private sealed class SequentialRandomNumberGenerator : RandomNumberGenerator
        {
            private byte _value;

            public override void GetBytes(byte[] data)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = _value++;
                }
            }
        }

        private class PasswordHasherOptionsAccessor : IOptions<PasswordHasherOptions>
        {
            public PasswordHasherOptions Value { get; } = new PasswordHasherOptions();
        }

    }
}