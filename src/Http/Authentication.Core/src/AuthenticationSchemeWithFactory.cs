using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication
{
    internal class AuthenticationSchemeWithFactory : AuthenticationScheme
    {
        internal ObjectFactory ObjectFactory { get; }

        internal AuthenticationSchemeWithFactory(AuthenticationScheme scheme) :
            base(scheme.Name, scheme.DisplayName, scheme.HandlerType)
        {
            ObjectFactory = ActivatorUtilities.CreateFactory(scheme.HandlerType, Type.EmptyTypes);
        }
    }
}
