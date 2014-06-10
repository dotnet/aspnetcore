// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Test
{
    /// <summary>
    /// An implementation of DefaultActionDiscoveryConventions that only allows controllers
    /// from a fixed set of types.
    /// </summary>
    public class StaticActionDiscoveryConventions : DefaultActionDiscoveryConventions
    {
        public StaticActionDiscoveryConventions(params TypeInfo[] controllerTypes)
        {
            ControllerTypes = new List<TypeInfo>(controllerTypes ?? Enumerable.Empty<TypeInfo>());
        }

        public List<TypeInfo> ControllerTypes { get; private set; }

        public override bool IsController([NotNull]TypeInfo typeInfo)
        {
            return ControllerTypes.Contains(typeInfo);
        }
    }
}