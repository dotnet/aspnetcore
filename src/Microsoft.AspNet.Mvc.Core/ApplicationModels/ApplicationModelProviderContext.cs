// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// A context object for <see cref="IApplicationModelProvider"/>.
    /// </summary>
    public class ApplicationModelProviderContext
    {
        public ApplicationModelProviderContext([NotNull] IEnumerable<TypeInfo> controllerTypes)
        {
            ControllerTypes = controllerTypes;
        }

        public IEnumerable<TypeInfo> ControllerTypes { get; }

        /// <summary>
        /// Gets the <see cref="ApplicationModel"/>.
        /// </summary>
        public ApplicationModel Result { get; } = new ApplicationModel();
    }
}