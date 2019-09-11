// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
    internal class IdentityServerJwtDescriptor : IIdentityServerJwtDescriptor
    {
        public IdentityServerJwtDescriptor(IHostingEnvironment environment)
        {
            Environment = environment;
        }

        public IHostingEnvironment Environment { get; }

        public IDictionary<string, ResourceDefinition> GetResourceDefinitions()
        {
            return new Dictionary<string, ResourceDefinition>
            {
                [Environment.ApplicationName + "API"] = new ResourceDefinition() { Profile = ApplicationProfiles.IdentityServerJwt }
            };
        }
    }
}
