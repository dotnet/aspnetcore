// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components
{
#pragma warning disable PUB0001 // Pubternal type in public API
    /// <summary>
    /// Displays differing content depending on the user's authorization status.
    /// </summary>
    public class AuthorizeView : Internal.AuthorizeViewCore
#pragma warning restore PUB0001 // Pubternal type in public API
    {
        private readonly IAuthorizeData[] selfAsAuthorizeData;

        /// <summary>
        /// Constructs an instance of <see cref="AuthorizeView"/>.
        /// </summary>
        public AuthorizeView()
        {
            selfAsAuthorizeData = new[] { new AuthorizeDataAdapter(this) };
        }

        /// <summary>
        /// The policy name that determines whether the content can be displayed.
        /// </summary>
        [Parameter] public string Policy { get; private set; }

        /// <summary>
        /// A comma delimited list of roles that are allowed to display the content.
        /// </summary>
        [Parameter] public string Roles { get; private set; }

        /// <summary>
        /// Gets the data used for authorization.
        /// </summary>
        protected override IAuthorizeData[] AuthorizeData => selfAsAuthorizeData;
    }
}
