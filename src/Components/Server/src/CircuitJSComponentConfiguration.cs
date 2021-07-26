// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class CircuitJSComponentConfiguration : IJSComponentConfiguration
    {
        public JSComponentConfigurationStore JSComponents { get; } = new();
    }
}
