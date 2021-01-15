// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// Interface that exposes a list of http methods that are supported by an provider.
    /// </summary>
    public interface IActionHttpMethodProvider
    {
        /// <summary>
        /// The list of http methods this action provider supports.
        /// </summary>
        IEnumerable<string> HttpMethods { get; }
    }
}
