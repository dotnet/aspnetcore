// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Authorization;

public sealed partial class AuthorizeRouteView
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    internal static Type GetAuthorizeRouteViewCoreType()
        => typeof(AuthorizeRouteViewCore);

    internal static AuthorizeViewCore CreateAuthorizeRouteViewCore()
        => new AuthorizeRouteViewCore();

    internal static RouteData GetAuthorizeRouteViewCoreRouteData(AuthorizeViewCore target)
        => ((AuthorizeRouteViewCore)target).RouteData;

    internal static void SetAuthorizeRouteViewCoreRouteData(AuthorizeViewCore target, RouteData value)
        => ((AuthorizeRouteViewCore)target).RouteData = value;
}
