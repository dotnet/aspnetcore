// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TwitterExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(authenticationScheme, TwitterDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TwitterOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>());
            return builder.AddRemoteScheme<TwitterOptions, TwitterHandler>(authenticationScheme, displayName, configureOptions);
        }

        public static async Task EnsureTwitterRequestSuccess(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Failure, attempt to parse Twitters error message
                var errorContentStream = await response.Content.ReadAsStreamAsync();

                try
                {
                    var errorResponse = await JsonSerializer.DeserializeAsync<TwitterErrorResponse>(errorContentStream, _jsonSerializerOptions);

                    throw TwitterException.FromTwitterErrorResponse(errorResponse);
                }
                catch
                {
                    // No valid Twitter error response, throw as normal
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
