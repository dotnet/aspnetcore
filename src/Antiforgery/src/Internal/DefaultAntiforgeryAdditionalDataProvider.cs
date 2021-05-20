// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery
{
    /// <summary>
    /// A default <see cref="IAntiforgeryAdditionalDataProvider"/> implementation.
    /// </summary>
    internal class DefaultAntiforgeryAdditionalDataProvider : IAntiforgeryAdditionalDataProvider
    {
        /// <inheritdoc />
        public string GetAdditionalData(HttpContext context)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public bool ValidateAdditionalData(HttpContext context, string additionalData)
        {
            // Default implementation does not understand anything but empty data.
            return string.IsNullOrEmpty(additionalData);
        }
    }
}
