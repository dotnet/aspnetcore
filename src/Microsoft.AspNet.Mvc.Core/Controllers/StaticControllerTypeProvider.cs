// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// A <see cref="IControllerTypeProvider"/> with a fixed set of types that are used as controllers. 
    /// </summary>
    public class StaticControllerTypeProvider : IControllerTypeProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="StaticControllerTypeProvider"/>.
        /// </summary>
        public StaticControllerTypeProvider()
            : this(Enumerable.Empty<TypeInfo>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StaticControllerTypeProvider"/>.
        /// </summary>
        /// <param name="controllerTypes">The sequence of controller <see cref="TypeInfo"/>.</param>
        public StaticControllerTypeProvider(IEnumerable<TypeInfo> controllerTypes)
        {
            if (controllerTypes == null)
            {
                throw new ArgumentNullException(nameof(controllerTypes));
            }

            ControllerTypes = new List<TypeInfo>(controllerTypes);
        }

        /// <summary>
        /// Gets the list of controller <see cref="TypeInfo"/>s.
        /// </summary>
        public IList<TypeInfo> ControllerTypes { get; }

        /// <inheritdoc />
        IEnumerable<TypeInfo> IControllerTypeProvider.ControllerTypes => ControllerTypes;
    }
}
