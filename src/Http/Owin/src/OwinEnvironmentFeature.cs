// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Owin
{
    /// <summary>
    /// Default implementation of <see cref="IOwinEnvironmentFeature"/>.
    /// </summary>
    public class OwinEnvironmentFeature : IOwinEnvironmentFeature
    {
        /// <inheritdoc />
        public IDictionary<string, object> Environment { get; set; }
    }
}
