// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of an <see cref="Assembly"/>. Logged during Assembly discovery in Startup.
    /// </summary>
    public class AssemblyValues : LoggerStructureBase
    {
        public AssemblyValues([NotNull] Assembly inner)
        {
            AssemblyName = inner.FullName;
#if ASPNET50
            Location = inner.Location;
#endif
            IsDynamic = inner.IsDynamic;
        }

        /// <summary>
        /// The name of the assembly. See <see cref="Assembly.FullName"/>.
        /// </summary>
        public string AssemblyName { get; }

#if ASPNET50
        /// <summary>
        /// The location of the assembly. See <see cref="Assembly.Location"/>.
        /// </summary>
        public string Location { get; }
#endif

        /// <summary>
        /// Whether or not the assembly is dynamic. See <see cref="Assembly.IsDynamic"/>.
        /// </summary>
        public bool IsDynamic { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}