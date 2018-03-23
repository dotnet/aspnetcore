// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// An <see cref="ApplicationPartFactory"/> that produces no parts.
    /// <para>
    /// This factory may be used to to preempt Mvc's default part discovery allowing for custom configuration at a later stage.
    /// </para>
    /// </summary>
    public class NullApplicationPartFactory : ApplicationPartFactory
    {
        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            return Enumerable.Empty<ApplicationPart>();
        }
    }
}
