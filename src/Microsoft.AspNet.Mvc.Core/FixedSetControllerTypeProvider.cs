// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IControllerTypeProvider"/> with a fixed set of types that are used as controllers. 
    /// </summary>
    public class FixedSetControllerTypeProvider : IControllerTypeProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FixedSetControllerTypeProvider"/>.
        /// </summary>
        public FixedSetControllerTypeProvider()
            : this(Enumerable.Empty<TypeInfo>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FixedSetControllerTypeProvider"/>.
        /// </summary>
        /// <param name="controllerTypes">The sequence of controller <see cref="TypeInfo"/>.</param>
        public FixedSetControllerTypeProvider([NotNull] IEnumerable<TypeInfo> controllerTypes)
        {
            ControllerTypes = new List<TypeInfo>(controllerTypes);
        }

        /// <summary>
        /// Gets the list of controller <see cref="TypeInfo"/>s.
        /// </summary>
        public IList<TypeInfo> ControllerTypes { get; }

        IEnumerable<TypeInfo> IControllerTypeProvider.ControllerTypes => ControllerTypes;
    }
}
