// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenHasherTest
    {
        public static TheoryData<string, string> WellKnownHashes =>
            new TheoryData<string, string>()
            {
                { "Qcb0Orv1zh30vL1MPRsbm-diHiMwcLyZvn1arpZv-Jxf_11jnpEX3Tgfvk", "LDktKdoQak3Pk0cnXxCltA" },
                { "jHkWEdUXMU1BwAsC4vtUsZwnNvTIxEl0z9K3vx5KF0Y", "77QmUPtjPfzWtF2AnpK9RQ" }
            };

        [Theory]
        [MemberData(nameof(WellKnownHashes))]
        public void TokenHasher_CanHashTokensSignedWithRS256(string input, string expectedHash)
        {
            // Arrange
            var hasher = new TokenHasher();

            // Act
            var hash = hasher.HashToken(input, "RS256");

            // Assert
            Assert.Equal(expectedHash, hash);
        }
    }
}
