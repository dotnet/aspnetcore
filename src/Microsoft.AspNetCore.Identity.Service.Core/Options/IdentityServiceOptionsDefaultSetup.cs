// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Service.Serialization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceOptionsDefaultSetup : IConfigureOptions<IdentityServiceOptions>
    {
        public void Configure(IdentityServiceOptions options)
        {
            options.LoginPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.SessionPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(IdentityServiceOptions.CookieAuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.ManagementPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(IdentityServiceOptions.CookieAuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.SerializationSettings = CreateDefault();
            options.SerializationSettings.Converters.Insert(0, new AuthorizationCodeConverter());
            options.SerializationSettings.Converters.Insert(0, new RefreshTokenConverter());
            options.AuthorizationCodeOptions = CreateAuthorizationCodeOptions(TimeSpan.FromMinutes(5), TimeSpan.Zero);

            options.AccessTokenOptions = CreateAccessTokenOptions(TimeSpan.FromHours(2), TimeSpan.Zero);
            options.RefreshTokenOptions = CreateRefreshTokenOptions(TimeSpan.FromDays(30), TimeSpan.Zero);
            options.IdTokenOptions = CreateIdTokenOptions(TimeSpan.FromHours(2), TimeSpan.Zero);
        }

        private static TokenOptions CreateAuthorizationCodeOptions(TimeSpan notValidAfter, TimeSpan notValidBefore)
        {
            var userClaims = new TokenMapping("user");
            userClaims.AddSingle(IdentityServiceClaimTypes.UserId, ClaimTypes.NameIdentifier);

            var applicationClaims = new TokenMapping("application");
            applicationClaims.AddSingle(IdentityServiceClaimTypes.ClientId);

            return new TokenOptions()
            {
                UserClaims = userClaims,
                ApplicationClaims = applicationClaims,
                NotValidAfter = notValidAfter,
                NotValidBefore = notValidBefore
            };
        }

        private static TokenOptions CreateAccessTokenOptions(TimeSpan notValidAfter, TimeSpan notValidBefore)
        {
            var userClaims = new TokenMapping("user");
            userClaims.AddSingle(IdentityServiceClaimTypes.Subject, ClaimTypes.NameIdentifier);

            var applicationClaims = new TokenMapping("application");

            return new TokenOptions()
            {
                UserClaims = userClaims,
                ApplicationClaims = applicationClaims,
                NotValidAfter = notValidAfter,
                NotValidBefore = notValidBefore
            };
        }

        private static TokenOptions CreateRefreshTokenOptions(TimeSpan notValidAfter, TimeSpan notValidBefore)
        {
            var userClaims = new TokenMapping("user");
            userClaims.AddSingle(IdentityServiceClaimTypes.UserId, ClaimTypes.NameIdentifier);

            var applicationClaims = new TokenMapping("application");
            applicationClaims.AddSingle(IdentityServiceClaimTypes.ClientId, IdentityServiceClaimTypes.ClientId);

            return new TokenOptions()
            {
                UserClaims = userClaims,
                ApplicationClaims = applicationClaims,
                NotValidAfter = notValidAfter,
                NotValidBefore = notValidBefore
            };
        }

        private static TokenOptions CreateIdTokenOptions(TimeSpan notValidAfter, TimeSpan notValidBefore)
        {
            var userClaims = new TokenMapping("user");

            var applicationClaims = new TokenMapping("application");
            applicationClaims.AddSingle(IdentityServiceClaimTypes.Audience, IdentityServiceClaimTypes.ClientId);

            return new TokenOptions()
            {
                UserClaims = userClaims,
                ApplicationClaims = applicationClaims,
                NotValidAfter = notValidAfter,
                NotValidBefore = notValidBefore
            };
        }

        private static JsonSerializerSettings CreateDefault() =>
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,

                // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
                // from deserialization errors that might occur from deeply nested objects.
                MaxDepth = 32,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
                TypeNameHandling = TypeNameHandling.None,
            };
    }
}
