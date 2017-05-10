// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Claims
{
    public class TimeStampTokenClaimsProviderTest
    {
        public static TheoryData<string, string, string, string> ExpectedTimeStampsData =>
            new TheoryData<string, string, string, string>
            {
                { TokenTypes.AuthorizationCode, "946684800", "946681200","946688400" },
                { TokenTypes.AccessToken, "946684800", "946677600", "946692000" },
                { TokenTypes.IdToken, "946684800", "946674000", "946695600" },
                { TokenTypes.RefreshToken, "946684800", "946670400", "946699200" },
            };

        [Theory]
        [MemberData(nameof(ExpectedTimeStampsData))]
        public async Task OnGeneratingClaims_AddsIssuedAtNotBeforeAndExpires_ForAllTokenTypes(
            string tokenType,
            string issuedAt,
            string notBefore,
            string expires)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage { },
                new RequestGrants { });

            // Reference time
            var reference = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.Zero);

            var timestampManager = new TestTimeStampManager(reference);
            var options = new IdentityServiceOptions();
            SetTimeStampOptions(options.AuthorizationCodeOptions, 1);
            SetTimeStampOptions(options.AccessTokenOptions, 2);
            SetTimeStampOptions(options.IdTokenOptions, 3);
            SetTimeStampOptions(options.RefreshTokenOptions, 4);

            var claimsProvider = new TimestampsTokenClaimsProvider(timestampManager, Options.Create(options));
            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.IssuedAt) && c.Value.Equals(issuedAt));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.NotBefore) && c.Value.Equals(notBefore));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Expires) && c.Value.Equals(expires));
        }

        private void SetTimeStampOptions(TokenOptions tokenOptions, int hours)
        {
            tokenOptions.NotValidAfter = TimeSpan.FromHours(hours);
            tokenOptions.NotValidBefore = TimeSpan.FromHours(-hours);
        }

        private class TestTimeStampManager : TimeStampManager
        {
            private readonly DateTimeOffset _reference;

            public TestTimeStampManager(DateTimeOffset reference)
            {
                _reference = reference;
            }

            public override DateTimeOffset GetCurrentTimeStampUtc()
            {
                return _reference;
            }
        }
    }
}
