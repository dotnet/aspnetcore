// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Authorization;

using WellKnownType = WellKnownTypeData.WellKnownType;

internal sealed class AuthorizationOptionsTypes
{
    public AuthorizationOptionsTypes(WellKnownTypes wellKnownTypes)
    {
        AuthorizationOptions = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Authorization_AuthorizationOptions);

        if (AuthorizationOptions is not null)
        {
            var authorizationOptionsMembers = AuthorizationOptions.GetMembers();

            var authorizationOptionsProperties = authorizationOptionsMembers.OfType<IPropertySymbol>();

            DefaultPolicy = authorizationOptionsProperties
                .FirstOrDefault(member => member.Name == "DefaultPolicy");
            FallbackPolicy = authorizationOptionsProperties
                .FirstOrDefault(member => member.Name == "FallbackPolicy");
            InvokeHandlersAfterFailure = authorizationOptionsProperties
                .FirstOrDefault(member => member.Name == "InvokeHandlersAfterFailure");

            GetPolicy = authorizationOptionsMembers.OfType<IMethodSymbol>()
                .FirstOrDefault(member => member.Name == "GetPolicy");
        }
    }

    public INamedTypeSymbol? AuthorizationOptions { get; }
    public IPropertySymbol? DefaultPolicy { get; }
    public IPropertySymbol? FallbackPolicy { get; }
    public IPropertySymbol? InvokeHandlersAfterFailure { get; }
    public IMethodSymbol? GetPolicy { get; }

    public bool HasRequiredTypes => AuthorizationOptions is not null
        && DefaultPolicy is not null
        && FallbackPolicy is not null
        && InvokeHandlersAfterFailure is not null
        && GetPolicy is not null;
}
