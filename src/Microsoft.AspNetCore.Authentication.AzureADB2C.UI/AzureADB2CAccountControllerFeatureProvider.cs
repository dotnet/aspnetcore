// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2C.Controllers.Internal;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal class AzureADB2CAccountControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>, IApplicationFeatureProvider
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            if (!feature.Controllers.Contains(typeof(AccountController).GetTypeInfo()))
            {
                feature.Controllers.Add(typeof(AccountController).GetTypeInfo());
            }
        }
    }
}