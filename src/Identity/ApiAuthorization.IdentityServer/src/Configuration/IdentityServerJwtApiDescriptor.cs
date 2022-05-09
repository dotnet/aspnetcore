// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;

internal sealed class IdentityServerJwtDescriptor : IIdentityServerJwtDescriptor
{
    public IdentityServerJwtDescriptor(IWebHostEnvironment environment)
    {
        Environment = environment;
    }

    public IWebHostEnvironment Environment { get; }

    public IDictionary<string, ResourceDefinition> GetResourceDefinitions()
    {
        return new Dictionary<string, ResourceDefinition>
        {
            [Environment.ApplicationName + "API"] = new ResourceDefinition() { Profile = ApplicationProfiles.IdentityServerJwt }
        };
    }
}
