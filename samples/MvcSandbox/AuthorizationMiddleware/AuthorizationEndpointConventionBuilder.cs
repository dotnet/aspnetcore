// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;

namespace MvcSandbox.AuthorizationMiddleware
{
    public static class AuthorizationEndpointConventionBuilder
    {
        public static T RequireAuthorization<T>(this T builder, params string[] roles) where T : IEndpointConventionBuilder
        {
            builder.Apply(model => model.Metadata.Add(new AuthorizeMetadataAttribute(roles)));
            return builder;
        }
    }
}
