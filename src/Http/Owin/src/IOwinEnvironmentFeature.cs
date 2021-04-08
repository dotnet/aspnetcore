// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Owin
{
    /// <summary>
    /// A feature interface for an OWIN environment.
    /// </summary>
    public interface IOwinEnvironmentFeature
    {
        /// <summary>
        /// Gets or sets the environment values.
        /// </summary>
        IDictionary<string, object> Environment { get; set; }
    }
}
