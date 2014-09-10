// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Provides extensions methods for app.Property values that are only needed by implementations of authentication middleware.
    /// </summary>
    public static class BuilderSecurityExtensions
    {
        /// <summary>
        /// Returns the previously set AuthenticationType that external sign in middleware should use when the
        /// browser navigates back to their return url.
        /// </summary>
        /// <param name="app">App builder passed to the application startup code</param>
        /// <returns></returns>
        public static string GetDefaultSignInAsAuthenticationType([NotNull] this IApplicationBuilder app)
        {
            object value;
            if (app.Properties.TryGetValue(Constants.DefaultSignInAsAuthenticationType, out value))
            {
                var authenticationType = value as string;
                if (!string.IsNullOrEmpty(authenticationType))
                {
                    return authenticationType;
                }
            }
            throw new InvalidOperationException(Resources.Exception_MissingDefaultSignInAsAuthenticationType);
        }

        /// <summary>
        /// Called by middleware to change the name of the AuthenticationType that external middleware should use
        /// when the browser navigates back to their return url.
        /// </summary>
        /// <param name="app">App builder passed to the application startup code</param>
        /// <param name="authenticationType">AuthenticationType that external middleware should sign in as.</param>
        public static void SetDefaultSignInAsAuthenticationType([NotNull] this IApplicationBuilder app, [NotNull] string authenticationType)
        {
            app.Properties[Constants.DefaultSignInAsAuthenticationType] = authenticationType;
        }
    }
}