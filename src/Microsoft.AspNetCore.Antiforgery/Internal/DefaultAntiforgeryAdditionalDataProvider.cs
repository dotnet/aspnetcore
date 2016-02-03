// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    /// <summary>
    /// A default <see cref="IAntiforgeryAdditionalDataProvider"/> implementation.
    /// </summary>
    public class DefaultAntiforgeryAdditionalDataProvider : IAntiforgeryAdditionalDataProvider
    {
        /// <inheritdoc />
        public virtual string GetAdditionalData(HttpContext context)
        {
            return string.Empty;
        }

        /// <inheritdoc />
        public virtual bool ValidateAdditionalData(HttpContext context, string additionalData)
        {
            // Default implementation does not understand anything but empty data.
            return string.IsNullOrEmpty(additionalData);
        }
    }
}