// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Test;

public class PasswordHasherTest
{
    // Password used in these tests
    public const string Plaintext_Password = "my password";

    // V2 Hashed versions of Plaintext_Password
    public const string V2_SHA1_1000iter_128salt_256subkey = "AAABAgMEBQYHCAkKCwwNDg+ukCEMDf0yyQ29NYubggHIVY0sdEUfdyeM+E1LtH1uJg==";

    // V3 Hashed versions of Plaintext_Password
    public const string V3_SHA1_250iter_128salt_128subkey = "AQAAAAAAAAD6AAAAEAhftMyfTJylOlZT+eEotFXd1elee8ih5WsjXaR3PA9M";
    public const string V3_SHA256_250000iter_256salt_256subkey = "AQAAAAEAA9CQAAAAIESkQuj2Du8Y+kbc5lcN/W/3NiAZFEm11P27nrSN5/tId+bR1SwV8CO1Jd72r4C08OLvplNlCDc3oQZ8efcW+jQ=";
    public const string V3_SHA512_50iter_128salt_128subkey = "AQAAAAIAAAAyAAAAEOMwvh3+FZxqkdMBz2ekgGhwQ4B6pZWND6zgESBuWiHw";
    public const string V3_SHA512_250iter_256salt_512subkey = "AQAAAAIAAAD6AAAAIJbVi5wbMR+htSfFp8fTw8N8GOS/Sje+S/4YZcgBfU7EQuqv4OkVYmc4VJl9AGZzmRTxSkP7LtVi9IWyUxX8IAAfZ8v+ZfhjCcudtC1YERSqE1OEdXLW9VukPuJWBBjLuw==";
    public const string V3_SHA512_10000iter_128salt_256subkey = "AQAAAAIAACcQAAAAEAABAgMEBQYHCAkKCwwNDg9B0Oxwty+PGIDSp95gcCfzeDvA4sGapUIUov8usXfD6A==";
    public const string V3_SHA512_100000iter_128salt_256subkey = "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg/Q8A0WMKbtHQJQ2DHCdoEeeFBrgNlldq6vH4qX/CGqGQ==";

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
        string retVal = hasher.HashPassword(null, Plaintext_Password);

        // Assert
        Assert.Equal(V3_SHA512_100000iter_128salt_256subkey, retVal);
    }

    [Fact]
    public void HashPassword_Version2()
    {
        // Arrange
        var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV2);

        // Act
        string retVal = hasher.HashPassword(null, Plaintext_Password);

        // Assert
        Assert.Equal(V2_SHA1_1000iter_128salt_256subkey, retVal);
    }

    [Fact]
    public void HashPassword_Version3()
    {
        // Arrange
        var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV3);

        // Act
        string retVal = hasher.HashPassword(null, Plaintext_Password);

        // Assert
        Assert.Equal(V3_SHA512_100000iter_128salt_256subkey, retVal);
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
        var result = hasher.VerifyHashedPassword(null, hashedPassword, Plaintext_Password);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Theory]
    // Version 2 payloads
    [InlineData(V2_SHA1_1000iter_128salt_256subkey)]
    // Version 3 payloads
    [InlineData(V3_SHA512_50iter_128salt_128subkey)]
    [InlineData(V3_SHA512_250iter_256salt_512subkey)]
    [InlineData(V3_SHA512_100000iter_128salt_256subkey)]
    public void VerifyHashedPassword_Version2CompatMode_SuccessCases(string hashedPassword)
    {
        // Arrange
        var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV2);

        // Act
        var result = hasher.VerifyHashedPassword(null, hashedPassword, Plaintext_Password);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Theory]
    // Version 2 payloads
    [InlineData(V2_SHA1_1000iter_128salt_256subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    // Version 3 payloads
    [InlineData(V3_SHA1_250iter_128salt_128subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    [InlineData(V3_SHA256_250000iter_256salt_256subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    [InlineData(V3_SHA512_50iter_128salt_128subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    [InlineData(V3_SHA512_250iter_256salt_512subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    [InlineData(V3_SHA512_10000iter_128salt_256subkey, PasswordVerificationResult.SuccessRehashNeeded)]
    [InlineData(V3_SHA512_100000iter_128salt_256subkey, PasswordVerificationResult.Success)]
    public void VerifyHashedPassword_Version3CompatMode_SuccessCases(string hashedPassword, PasswordVerificationResult expectedResult)
    {
        // Arrange
        var hasher = new PasswordHasher(compatMode: PasswordHasherCompatibilityMode.IdentityV3);

        // Act
        var actualResult = hasher.VerifyHashedPassword(null, hashedPassword, Plaintext_Password);

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
