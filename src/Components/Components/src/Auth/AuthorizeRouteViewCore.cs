// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Auth;

namespace Microsoft.AspNetCore.Components
{
    public class AuthorizeRouteViewCore : AuthorizeViewCore
    {
        /// <summary>
        /// Gets or sets the route data.
        /// </summary>
        [Parameter]
        public ComponentRouteData RouteData { get; set; }

        protected override IAuthorizeData[] GetAuthorizeData()
            => AttributeAuthorizeDataCache.GetAuthorizeDataForType(RouteData.PageComponentType);
    }
}
