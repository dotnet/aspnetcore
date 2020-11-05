// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// State for the Challenge event.
    /// </summary>
    public class ChallengeContext : PropertiesContext<NegotiateOptions>
    {
        /// <summary>
        /// Creates a new <see cref="ChallengeContext"/>.
        /// </summary>
        /// <inheritdoc />
        public ChallengeContext(
            HttpContext context,
            AuthenticationScheme scheme,
            NegotiateOptions options,
            AuthenticationProperties properties)
            : base(context, scheme, options, properties) { }

        /// <summary>
        /// Gets a value that determines if this challenge was handled.
        /// If <see langword="true"/>, will skip any default logic for this challenge.
        /// </summary>
        public bool Handled { get; private set; }

        /// <summary>
        /// Skips any default logic for this challenge.
        /// </summary>
        public void HandleResponse() => Handled = true;
    }
}
