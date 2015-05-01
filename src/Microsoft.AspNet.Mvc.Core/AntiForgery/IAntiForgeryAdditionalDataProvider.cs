// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Allows providing or validating additional custom data for anti-forgery tokens.
    /// For example, the developer could use this to supply a nonce when the token is
    /// generated, then he could validate the nonce when the token is validated.
    /// </summary>
    /// <remarks>
    /// The anti-forgery system already embeds the client's username within the
    /// generated tokens. This interface provides and consumes <em>supplemental</em>
    /// data. If an incoming anti-forgery token contains supplemental data but no
    /// additional data provider is configured, the supplemental data will not be
    /// validated.
    /// </remarks>
    public interface IAntiForgeryAdditionalDataProvider
    {
        /// <summary>
        /// Provides additional data to be stored for the anti-forgery tokens generated
        /// during this request.
        /// </summary>
        /// <param name="context">Information about the current request.</param>
        /// <returns>Supplemental data to embed within the anti-forgery token.</returns>
        string GetAdditionalData(HttpContext context);

        /// <summary>
        /// Validates additional data that was embedded inside an incoming anti-forgery
        /// token.
        /// </summary>
        /// <param name="context">Information about the current request.</param>
        /// <param name="additionalData">Supplemental data that was embedded within the token.</param>
        /// <returns>True if the data is valid; false if the data is invalid.</returns>
        bool ValidateAdditionalData(HttpContext context, string additionalData);
    }
}