// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An implementation of IAssemblyProvider that provides just this assembly.
    /// </summary>
    public class StaticAssemblyProvider : IAssemblyProvider
    {
        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                yield return typeof(StaticActionDiscoveryConventions).GetTypeInfo().Assembly;
            }
        }
    }
}