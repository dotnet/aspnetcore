// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization;

// This is so the AuthorizeView can avoid implementing IAuthorizeData (even privately)
internal sealed class AuthorizeDataAdapter : IAuthorizeData
{
    private readonly AuthorizeView _component;

    public AuthorizeDataAdapter(AuthorizeView component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

    public string? Policy
    {
        get => _component.Policy;
        set => throw new NotSupportedException();
    }

    public string? Roles
    {
        get => _component.Roles;
        set => throw new NotSupportedException();
    }

    // AuthorizeView doesn't expose any such parameter, as it wouldn't be used anyway,
    // since we already have the ClaimsPrincipal by the time AuthorizeView gets involved.
    public string? AuthenticationSchemes
    {
        get => null;
        set => throw new NotSupportedException();
    }
}
