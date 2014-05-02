// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Abstractions;

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