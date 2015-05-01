// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ControllerDiscoveryConventionsWebSite
{
    public abstract class AbstractController
    {
        public string GetValue()
        {
            return nameof(AbstractController);
        }
    }
}