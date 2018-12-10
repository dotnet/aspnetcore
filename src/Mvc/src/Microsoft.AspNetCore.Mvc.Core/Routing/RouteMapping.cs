// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents a conventional route that's been added via <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    internal class RouteMapping
    {
        public string Name { get; set; }

        public string Template { get; set; }

        public object Defaults { get; set; }

        public object Constraints { get; set; }

        public object DataTokens { get; set; }
    }
}
