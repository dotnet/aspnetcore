// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// Specifies the contract for discovering assemblies that may contain Mvc specific types such as controllers,
    /// view components and precompiled views.
    /// </summary>
    public interface IAssemblyProvider
    {
        /// <summary>
        /// Gets the sequence of candidate <see cref="Assembly"/> instances that the application
        /// uses for discovery of Mvc specific types.
        /// </summary>
        IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}
